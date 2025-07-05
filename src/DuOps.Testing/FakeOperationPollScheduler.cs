// using System.Collections.Concurrent;
// using DuOps.Core.OperationDefinitions;
// using DuOps.Core.OperationPollers;
// using DuOps.Core.Operations;
// using DuOps.Core.PollingSchedule;
// using DuOps.Core.Registry;
// using DuOps.Core.Repositories;
//
// namespace DuOps.Testing;
//
// public sealed class FakeOperationPollScheduler(
//     IOperationDefinitionRegistry operationDefinitionRegistry,
//     IOperationStorage operationStorage,
//     IOperationPoller operationPoller,
//     IServiceProvider serviceProvider
// ) : IOperationPollingScheduler
// {
//     public readonly ConcurrentDictionary<
//         OperationPollingScheduleId,
//         (OperationDiscriminator Discriminator, OperationId OperationId)
//     > ScheduledOperations = new();
//
//     public Task<OperationPollingScheduleId> SchedulePolling<TArgs, TResult>(
//         IOperationDefinition<TArgs, TResult> operationDefinition,
//         OperationId operationId,
//         CancellationToken cancellationToken
//     )
//     {
//         var scheduleId = new OperationPollingScheduleId(Guid.NewGuid().ToString());
//
//         ScheduledOperations[scheduleId] = (operationDefinition.Discriminator, operationId);
//
//         return Task.FromResult(scheduleId);
//     }
//
//     public Task Poll(OperationPollingScheduleId scheduleId, CancellationToken cancellationToken)
//     {
//         var (discriminator, operationId) = ScheduledOperations[scheduleId];
//     }
// }
