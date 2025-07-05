using DuOps.Core.OperationDefinitions;
using DuOps.Core.OperationPollers;
using DuOps.Core.Operations;
using DuOps.Core.PollingSchedule;
using DuOps.Core.Registry;
using DuOps.Core.Storages;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.DependencyInjection;

namespace DuOps.Hangfire;

public sealed class HangfireOperationPollingScheduler(
    IOperationDefinitionRegistry operationDefinitionRegistry,
    IBackgroundJobClientV2 backgroundJobClient,
    IOperationStorage operationStorage,
    IOperationPoller operationPoller,
    IServiceProvider serviceProvider
) : IOperationPollingScheduler
{
    public Task<OperationPollingScheduleId> SchedulePolling<TArgs, TResult>(
        IOperationDefinition<TArgs, TResult> operationDefinition,
        OperationId operationId,
        CancellationToken cancellationToken
    )
    {
        var id = backgroundJobClient.Enqueue<HangfireOperationPollingScheduler>(x =>
            x.HangfireCallback(
                operationDefinition.Discriminator.Value,
                operationId.ShardKey,
                operationId.Value,
                null!,
                CancellationToken.None
            )
        );

        return Task.FromResult(new OperationPollingScheduleId(id));
    }

    public async Task HangfireCallback(
        string operationDiscriminator,
        string? operationIdShardKey,
        string operationIdValue,
        PerformContext performContext,
        CancellationToken cancellationToken
    )
    {
        var backgroundJobId = performContext.BackgroundJob.Id;
        var pollingScheduleId = new OperationPollingScheduleId(backgroundJobId);
        var discriminator = new OperationDiscriminator(operationDiscriminator);
        var operationId = new OperationId(operationIdShardKey, operationIdValue);

        await operationDefinitionRegistry.InvokeCallbackWithDefinition<
            TypedHangfireCallbackProxy,
            object?
        >(
            discriminator,
            new TypedHangfireCallbackProxy(this, operationId, pollingScheduleId, cancellationToken)
        );
    }

    private async Task TypedHangfireCallback<TArgs, TResult>(
        IOperationDefinition<TArgs, TResult> operationDefinition,
        OperationId operationId,
        OperationPollingScheduleId pollingScheduleId,
        CancellationToken cancellationToken
    )
    {
        using var serviceScope = serviceProvider.CreateScope();

        var operation = await GetOperation(operationDefinition, operationId, cancellationToken);

        // TODO: Check PollingScheduleId

        var result = await operationPoller.PollOperation(
            operationDefinition,
            operation,
            cancellationToken
        );

        if (result is OperationState<TResult>.Yielded)
        {
            throw new Exception(
                $"Operation({operationDefinition.Discriminator.Value};{operationId.ShardKey};{operationId.Value}) yielded"
            );
        }
    }

    private async Task<SerializedOperation> GetOperation<TArgs, TResult>(
        IOperationDefinition<TArgs, TResult> operationDefinition,
        OperationId operationId,
        CancellationToken cancellationToken
    )
    {
        const int attempts = 10;
        var attemptsInterval = TimeSpan.FromMilliseconds(10);

        for (int i = 0; i < attempts; i++)
        {
            var serializedOperation = await operationStorage.GetByIdOrDefault(
                operationDefinition.Discriminator,
                operationId,
                cancellationToken
            );

            if (serializedOperation is not null)
            {
                return serializedOperation;
            }

            await Task.Delay(attemptsInterval, cancellationToken);
        }

        throw new InvalidOperationException(
            $"Operation({operationDefinition.Discriminator.Value};{operationId.ShardKey};{operationId.Value}) was not found"
        );
    }

    private readonly struct TypedHangfireCallbackProxy(
        HangfireOperationPollingScheduler pollingScheduler,
        OperationId operationId,
        OperationPollingScheduleId pollingScheduleId,
        CancellationToken cancellationToken
    ) : IOperationDefinitionGenericCallback<object?>
    {
        public async Task<object?> Invoke<TArgs, TResult>(
            IOperationDefinition<TArgs, TResult> operationDefinition
        )
        {
            await pollingScheduler.TypedHangfireCallback(
                operationDefinition,
                operationId,
                pollingScheduleId,
                cancellationToken
            );

            return null;
        }
    }
}
