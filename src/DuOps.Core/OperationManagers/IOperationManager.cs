using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;

namespace DuOps.Core.OperationManagers;

public interface IOperationManager
{
    Task<Operation<TArgs, TResult>> StartInBackground<TArgs, TResult>(
        IOperationDefinition<TArgs, TResult> definition,
        Operation<TArgs, TResult> operation,
        CancellationToken cancellationToken
    );
}
