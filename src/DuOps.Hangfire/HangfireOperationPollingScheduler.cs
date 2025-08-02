using DuOps.Core.OperationDefinitions;
using DuOps.Core.OperationPollers;
using DuOps.Core.Operations;
using DuOps.Core.PollingSchedule;
using DuOps.Core.Storages;
using Hangfire;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.States;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DuOps.Hangfire;

internal sealed class HangfireOperationPollingScheduler(
    IBackgroundJobClientV2 backgroundJobClient,
    IServiceProvider serviceProvider,
    ILogger<HangfireOperationPollingScheduler> logger
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
        string operationIdValue,
        PerformContext performContext,
        CancellationToken cancellationToken
    )
    {
        var discriminator = new OperationDiscriminator(operationDiscriminator);
        var operationId = new OperationId(operationIdValue);

        using var serviceScope = serviceProvider.CreateScope();
        var operationPoller = serviceScope.ServiceProvider.GetRequiredService<IOperationPoller>();
        var operationStorage = serviceScope.ServiceProvider.GetRequiredService<IOperationStorage>();

        var operation = await GetOperation(
            operationStorage,
            discriminator,
            operationId,
            cancellationToken
        );

        var jobId = performContext.BackgroundJob.Id;
        if (jobId != operation.PollingScheduleId)
        {
            logger.LogError(
                "Job({JobId}) stoped, because {OperationDiscriminator}({OperationId}).PollingScheduleId != JobId",
                jobId,
                operation.Discriminator,
                operationId
            );
            return;
        }

        var newOperationState = await operationPoller.PollOperation(operation, cancellationToken);

        switch (newOperationState)
        {
            // TODO: Handle state yielded

            case SerializedOperationState.Waiting waiting:
                performContext.SetOperationSchedule(waiting.Until);
                break;
            case SerializedOperationState.Retrying retrying:
                performContext.SetOperationSchedule(retrying.At);
                break;
        }
    }

    private async Task<SerializedOperation> GetOperation(
        IOperationStorage operationStorage,
        OperationDiscriminator discriminator,
        OperationId operationId,
        CancellationToken cancellationToken
    )
    {
        var attemptsInterval = TimeSpan.FromMilliseconds(10);

        var operation =
            await operationStorage.AwaitOperationHasPollingScheduleIdAndGetByIdOrDefault(
                discriminator,
                operationId,
                attemptsInterval,
                attemptsInterval * 10,
                cancellationToken
            );

        return operation
            ?? throw new InvalidOperationException(
                $"{discriminator.Value}({operationId}) was not found"
            );
    }

    private sealed class HangfireFilter : JobFilterAttribute, IElectStateFilter
    {
        public void OnStateElection(ElectStateContext context)
        {
            if (
                context.CandidateState is SucceededState
                && context.GetOperationSchedule() is { } newSchedule
            )
            {
                context.CandidateState = new ScheduledState(newSchedule.UtcDateTime);
            }
        }
    }
}

internal static class HangfireContextExtensions
{
    private const string OperationScheduleCustomDataKey = "DuOps.OperationSchedule";

    internal static void SetOperationSchedule(this PerformContext context, DateTimeOffset at)
    {
        context.Items[OperationScheduleCustomDataKey] = at;
    }

    internal static DateTimeOffset? GetOperationSchedule(this ElectStateContext context)
    {
        return context.CustomData.TryGetValue(OperationScheduleCustomDataKey, out var value)
            ? (DateTimeOffset?)value
            : null;
    }
}
