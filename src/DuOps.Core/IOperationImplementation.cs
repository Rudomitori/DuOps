using DuOps.Core.OperationPollers;

namespace DuOps.Core;

public interface IOperationImplementation<TArgs, TResult>
{
    Task<TResult> Execute(TArgs args, IOperationExecutionContext context);
}
