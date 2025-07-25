// using System.Collections.Concurrent;
// using DuOps.Core.Exceptions;
// using DuOps.Core.OperationDefinitions;
// using DuOps.Core.Operations;
// using DuOps.Core.Operations.InterResults;
// using DuOps.Core.Operations.InterResults.Definitions;
// using DuOps.Core.Storages;
//
// namespace DuOps.Testing;
//
// public sealed class InMemoryOperationStorage : IOperationStorage
// {
//     public readonly record struct OperationPk(
//         OperationDiscriminator Discriminator,
//         OperationId OperationId
//     );
//
//     public readonly ConcurrentDictionary<OperationPk, SerializedOperation> StoredOperations = new();
//
//     public readonly ConcurrentDictionary<
//         OperationPk,
//         Dictionary<
//             (InterResultDiscriminator Discriminator, SerializedInterResultKey? Key),
//             SerializedInterResult
//         >
//     > StoredIntermediateResults = new();
//
//     public Task<SerializedOperation?> GetByIdOrDefault(
//         OperationDiscriminator discriminator,
//         OperationId operationId,
//         CancellationToken cancellationToken = default
//     )
//     {
//         var operationPk = new OperationPk(discriminator, operationId);
//         var serializedOperation = StoredOperations.GetValueOrDefault(operationPk);
//         return Task.FromResult(serializedOperation);
//     }
//
//     public Task<SerializedOperation> GetOrAdd(
//         SerializedOperation serializedOperation,
//         CancellationToken cancellationToken = default
//     )
//     {
//         var operationPk = new OperationPk(
//             serializedOperation.Discriminator,
//             serializedOperation.Id
//         );
//
//         serializedOperation = StoredOperations.GetOrAdd(operationPk, serializedOperation);
//         return Task.FromResult(serializedOperation);
//     }
//
//     public Task<
//         IReadOnlyDictionary<
//             (InterResultDiscriminator Discriminator, SerializedInterResultKey? Key),
//             SerializedInterResult
//         >
//     > GetInterResults(
//         OperationDiscriminator discriminator,
//         OperationId operationId,
//         CancellationToken cancellationToken
//     )
//     {
//         var operationPk = new OperationPk(discriminator, operationId);
//
//         var results = StoredIntermediateResults.GetValueOrDefault(operationPk);
//         var serializedInterResults = results?.ToDictionary(x => x.Key, x => x.Value) ?? [];
//
//         return Task.FromResult<
//             IReadOnlyDictionary<
//                 (InterResultDiscriminator Discriminator, SerializedInterResultKey? Key),
//                 SerializedInterResult
//             >
//         >(serializedInterResults);
//     }
//
//     public Task AddInterResult(
//         OperationDiscriminator operationDiscriminator,
//         OperationId operationId,
//         InterResultDiscriminator interResultDiscriminator,
//         SerializedInterResultKey? key,
//         SerializedInterResult result,
//         CancellationToken cancellationToken
//     )
//     {
//         var operationPk = new OperationPk(operationDiscriminator, operationId);
//
//         var resultKey = key;
//
//         StoredIntermediateResults.AddOrUpdate(
//             operationPk,
//             _ => new Dictionary<
//                 (InterResultDiscriminator Discriminator, SerializedInterResultKey? Key),
//                 SerializedInterResult
//             >
//             {
//                 [(interResultDiscriminator, resultKey)] = result,
//             },
//             (_, results) =>
//             {
//                 if (!results.TryAdd((interResultDiscriminator, resultKey), result))
//                 {
//                     throw new InterResultConflictException(
//                         $"Operation({operationId}.IntermediateResults[{interResultDiscriminator}, {resultKey}] already exists"
//                     );
//                 }
//
//                 return results;
//             }
//         );
//
//         return Task.CompletedTask;
//     }
//
//     public Task AddResult(
//         OperationDiscriminator discriminator,
//         OperationId operationId,
//         SerializedOperationResult serializedOperationResult,
//         CancellationToken cancellationToken = default
//     )
//     {
//         var operationPk = new OperationPk(discriminator, operationId);
//
//         StoredOperations.AddOrUpdate(
//             operationPk,
//             _ =>
//                 throw new InvalidOperationException(
//                     $"Operation {discriminator.Value} {operationId.Value} was not found"
//                 ),
//             (_, operation) =>
//             {
//                 if (operation.State is SerializedOperationState.Finished)
//                 {
//                     throw new InvalidOperationException(
//                         $"Operation {discriminator.Value} {operationId.Value} already has result"
//                     );
//                 }
//
//                 return operation with
//                 {
//                     State = new SerializedOperationState.Finished(serializedOperationResult),
//                 };
//             }
//         );
//
//         return Task.CompletedTask;
//     }
//
//     public Task<OperationPollingScheduleId> GetOrSetPollingScheduleId(
//         OperationDiscriminator discriminator,
//         OperationId operationId,
//         OperationPollingScheduleId scheduleId,
//         CancellationToken cancellationToken
//     )
//     {
//         var operationPk = new OperationPk(discriminator, operationId);
//
//         var serializedOperation = StoredOperations.AddOrUpdate(
//             operationPk,
//             _ =>
//                 throw new InvalidOperationException(
//                     $"Operation {discriminator.Value} {operationId.Value} was not found"
//                 ),
//             (_, operation) =>
//             {
//                 if (operation.PollingScheduleId is not null)
//                 {
//                     return operation;
//                 }
//
//                 return operation with
//                 {
//                     PollingScheduleId = scheduleId,
//                 };
//             }
//         );
//
//         return Task.FromResult(serializedOperation.PollingScheduleId!.Value);
//     }
//
//     public Task Delete(
//         OperationDiscriminator discriminator,
//         OperationId operationId,
//         CancellationToken cancellationToken = default
//     )
//     {
//         throw new NotImplementedException();
//     }
// }
