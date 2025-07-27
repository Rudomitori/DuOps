using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;

namespace DuOps.Core.OperationPollers;

public interface IOperationPoller
{
    Task<SerializedOperationState> PollOperation(
        OperationDiscriminator operationDiscriminator,
        OperationId operationId,
        CancellationToken yieldToken = default
    );

    Task<SerializedOperationState> PollOperation(
        SerializedOperation serializedOperation,
        CancellationToken yieldToken = default
    );

    Task<OperationState<TResult>> PollOperation<TArgs, TResult>(
        IOperationDefinition<TArgs, TResult> operationDefinition,
        OperationId operationId,
        CancellationToken yieldToken = default
    );

    Task<OperationState<TResult>> PollOperation<TArgs, TResult>(
        IOperationDefinition<TArgs, TResult> operationDefinition,
        SerializedOperation serializedOperation,
        CancellationToken yieldToken = default
    );
}
