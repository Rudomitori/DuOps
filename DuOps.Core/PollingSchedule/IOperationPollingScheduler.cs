using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;

namespace DuOps.Core.PollingSchedule;

public interface IOperationPollingScheduler
{
    Task<OperationPollingScheduleId> SchedulePolling<TArgs, TResult>(
        IOperationDefinition<TArgs, TResult> operationDefinition,
        OperationId operationId,
        CancellationToken cancellationToken
    );
}
