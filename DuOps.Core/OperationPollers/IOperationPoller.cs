using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;

namespace DuOps.Core.OperationPollers;

public interface IOperationPoller
{
    Task<OperationExecutionResult<TResult>> PollOperation<TArgs, TResult>(
        IOperationDefinition<TArgs, TResult> operationDefinition,
        SerializedOperation serializedOperation,
        CancellationToken yieldToken = default
    );
}
