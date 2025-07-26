using DuOps.Core.OperationDefinitions;
using DuOps.Core.OperationPollers;
using DuOps.Core.Operations;
using DuOps.Core.PollingSchedule;
using DuOps.Core.Registry;
using DuOps.Core.Storages;
using Hangfire;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.States;
using Microsoft.Extensions.DependencyInjection;

namespace DuOps.Hangfire;

internal sealed class HangfireOperationPollingScheduler(
    IOperationDefinitionRegistry operationDefinitionRegistry,
    IBackgroundJobClientV2 backgroundJobClient,
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

    [HangfireFilter]
    public async Task HangfireCallback(
        string operationDiscriminator,
        string? operationIdShardKey,
        string operationIdValue,
        PerformContext performContext,
        CancellationToken cancellationToken
    )
    {
        var discriminator = new OperationDiscriminator(operationDiscriminator);
        var operationId = new OperationId(operationIdShardKey, operationIdValue);

        await operationDefinitionRegistry.InvokeCallbackWithDefinition<
            TypedHangfireCallbackProxy,
            object?
        >(
            discriminator,
            new TypedHangfireCallbackProxy(this, operationId, performContext, cancellationToken)
        );
    }

    private async Task TypedHangfireCallback<TArgs, TResult>(
        IOperationDefinition<TArgs, TResult> operationDefinition,
        OperationId operationId,
        PerformContext performContext,
        CancellationToken cancellationToken
    )
    {
        using var serviceScope = serviceProvider.CreateScope();
        var operationPoller = serviceScope.ServiceProvider.GetRequiredService<IOperationPoller>();
        var operationStorage = serviceScope.ServiceProvider.GetRequiredService<IOperationStorage>();

        var operation = await GetOperation(
            operationStorage,
            operationDefinition,
            operationId,
            cancellationToken
        );

        // TODO: Check PollingScheduleId

        var result = await operationPoller.PollOperation(
            operationDefinition,
            operation,
            cancellationToken
        );

        if (result is OperationState<TResult>.Yielded)
        {
            performContext.SetOperationYielded();
        }
    }

    private async Task<SerializedOperation> GetOperation<TArgs, TResult>(
        IOperationStorage operationStorage,
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
            $"{operationDefinition.Discriminator.Value}({operationId}) was not found"
        );
    }

    private readonly struct TypedHangfireCallbackProxy(
        HangfireOperationPollingScheduler pollingScheduler,
        OperationId operationId,
        PerformContext performContext,
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
                performContext,
                cancellationToken
            );

            return null;
        }
    }

    private sealed class HangfireFilter : JobFilterAttribute, IElectStateFilter
    {
        public void OnStateElection(ElectStateContext context)
        {
            if (context.CandidateState is SucceededState && context.GetOperationYielded())
            {
                context.CandidateState = new ScheduledState(TimeSpan.FromMilliseconds(100));
            }
        }
    }
}

internal static class HangfireContextExtensions
{
    private const string OperationYieldedCustomDataKey = "DuOps.OperationYielded";
    private const string OperationYieldedCustomDataValue = "true";

    internal static void SetOperationYielded(this PerformContext context)
    {
        context.Items[OperationYieldedCustomDataKey] = OperationYieldedCustomDataValue;
    }

    internal static bool GetOperationYielded(this ElectStateContext context)
    {
        return context.CustomData.TryGetValue(OperationYieldedCustomDataKey, out var value)
            && value is OperationYieldedCustomDataValue;
    }
}
