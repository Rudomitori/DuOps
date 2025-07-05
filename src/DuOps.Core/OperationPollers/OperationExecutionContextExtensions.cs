using DuOps.Core.Operations.InterResults.Definitions;

namespace DuOps.Core.OperationPollers;

public static class OperationExecutionContextExtensions
{
    public static async Task<TResult> RunWithCache<TResult>(
        this IOperationExecutionContext context,
        InterResultDiscriminator discriminator,
        Func<TResult, string> serialize,
        Func<string, TResult> deserialize,
        Func<Task<TResult>> action
    )
    {
        var definition = new AdHocInterResultDefinition<TResult>(discriminator, serialize, deserialize);

        return await context.RunWithCache(definition, action);
    }

    public static async Task<TResult> RunWithCache<TResult, TKey>(
        this IOperationExecutionContext context,
        InterResultDiscriminator discriminator,
        TKey key,
        Func<TResult, string> serialize,
        Func<string, TResult> deserialize,
        Func<TKey, string> serializeKey,
        Func<string, TKey> deserializeKey,
        Func<Task<TResult>> action
    )
    {
        var definition = new AdHocKeyedInterResultDefinition<TResult, TKey>(
            discriminator,
            serialize,
            deserialize,
            serializeKey,
            deserializeKey
        );

        return await context.RunWithCache(definition, key, action);
    }

    public static async Task RunWithCache(
        this IOperationExecutionContext context,
        InterResultDiscriminator discriminator,
        Func<Task> action
    )
    {
        var definition = new NullInterResultDefinition(discriminator);

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
