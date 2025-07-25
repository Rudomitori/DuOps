using System.Collections.Concurrent;
using DuOps.Core.OperationDefinitions;
using DuOps.Core.OperationPollers;
using DuOps.Core.Operations;
using DuOps.Core.PollingSchedule;
using Microsoft.Extensions.DependencyInjection;

namespace DuOps.Testing;

public sealed class FakeOperationPollScheduler(IServiceProvider serviceProvider)
    : IOperationPollingScheduler
{
    public readonly ConcurrentDictionary<
        OperationPollingScheduleId,
        (OperationDiscriminator Discriminator, OperationId OperationId)
    > ScheduledOperations = new();

    public Task<OperationPollingScheduleId> SchedulePolling<TArgs, TResult>(
        IOperationDefinition<TArgs, TResult> operationDefinition,
        OperationId operationId,
        CancellationToken cancellationToken
    )
    {
        var scheduleId = new OperationPollingScheduleId(Guid.NewGuid().ToString());

        ScheduledOperations[scheduleId] = (operationDefinition.Discriminator, operationId);

        return Task.FromResult(scheduleId);
    }

    public async Task Poll(
        OperationPollingScheduleId scheduleId,
        CancellationToken cancellationToken
    )
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var operationPoller = scope.ServiceProvider.GetRequiredService<IOperationPoller>();

        var (discriminator, operationId) = ScheduledOperations[scheduleId];
        await operationPoller.PollOperation(discriminator, operationId, cancellationToken);
    }
}
