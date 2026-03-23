using DuOps.Core.InnerResults;
using DuOps.Core.Serializers;
using InnerResultType = DuOps.Core.InnerResults.InnerResultType;

namespace DuOps.Core;

public static class OperationExecutionContextExtensions
{
    public static async Task<TValue> RunWithCache<TValue>(
        this IOperationExecutionContext context,
        InnerResultType type,
        ISerializer<TValue> serializer,
        Func<Task<TValue>> action
    )
    {
        var definition = new InnerResultDefinition<TValue>
        {
            Type = type,
            ValueSerializer = serializer,
        };

        return await context.RunWithCache(definition, action);
    }

    public static async Task<TValue> RunWithCache<TId, TValue>(
        this IOperationExecutionContext context,
        InnerResultType type,
        TId id,
        ISerializer<TValue> valueSerializer,
        ISerializer<TId> idSerializer,
        Func<Task<TValue>> action
    )
    {
        var definition = new InnerResultDefinition<TId, TValue>
        {
            Type = type,
            IdSerializer = idSerializer,
            ValueSerializer = valueSerializer,
        };

        return await context.RunWithCache(definition, id, action);
    }

    public static async Task RunWithCache(
        this IOperationExecutionContext context,
        InnerResultType type,
        Func<Task> action
    )
    {
        var definition = new InnerResultDefinition<object?>
        {
            Type = type,
            ValueSerializer = NullSerializer.Instance,
        };

        await context.RunWithCache(
            definition,
            async () =>
            {
                await action();
                return null;
            }
        );
    }
}
