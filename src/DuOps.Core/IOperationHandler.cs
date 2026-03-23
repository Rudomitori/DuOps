namespace DuOps.Core;

public interface IOperationHandler<TId, TArgs, TResult>
{
    Task<TResult> Execute(TId operationId, TArgs args, IOperationExecutionContext context);
}
