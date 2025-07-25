using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;
using DuOps.Core.PollingSchedule;
using DuOps.Core.Storages;
using DuOps.Core.Telemetry;

namespace DuOps.Core.OperationManagers;

internal sealed class OperationManager(
    IOperationPollingScheduler pollingScheduler,
    IOperationStorage storage,
    IOperationTelemetry telemetry
) : IOperationManager
{
    public async Task<Operation<TArgs, TResult>> StartInBackground<TArgs, TResult>(
        IOperationDefinition<TArgs, TResult> definition,
        Operation<TArgs, TResult> operation,
        CancellationToken cancellationToken = default
    )
    {
        var operationId = operation.Id;
        var serializedOperation = definition.Serialize(operation);

        serializedOperation = await storage.GetOrAdd(serializedOperation, cancellationToken);

        if (operation.PollingScheduleId is null)
        {
            var pollingScheduleId = await pollingScheduler.SchedulePolling(
                definition,
                operationId,
                cancellationToken
            );

            pollingScheduleId = await storage.GetOrSetPollingScheduleId(
                definition.Discriminator,
                operation.Id,
                pollingScheduleId,
                cancellationToken
            );

            serializedOperation = serializedOperation with
            {
                PollingScheduleId = pollingScheduleId,
            };
        }

        telemetry.OnOperationStartedInBackground(definition, serializedOperation);

        operation = definition.Deserialize(serializedOperation);
        return operation;
    }
}
