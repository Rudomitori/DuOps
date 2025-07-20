using DuOps.Core.Operations.InterResults.Definitions;

namespace DuOps.Core.OperationPollers;

public static class OperationExecutionContextExtensions
{
    public static async Task<TValue> RunWithCache<TValue>(
        this IOperationExecutionContext context,
        InterResultDiscriminator discriminator,
        Func<TValue, string> serialize,
        Func<string, TValue> deserialize,
        Func<Task<TValue>> action
    )
    {
        var definition = new AdHocInterResultDefinition<TValue>(
            discriminator,
            serialize,
            deserialize
        );

        return await context.RunWithCache(definition, action);
    }

    public static async Task<TValue> RunWithCache<TKey, TValue>(
        this IOperationExecutionContext context,
        InterResultDiscriminator discriminator,
        TKey key,
        Func<TValue, string> serialize,
        Func<string, TValue> deserialize,
        Func<TKey, string> serializeKey,
        Func<string, TKey> deserializeKey,
        Func<Task<TValue>> action
    )
    {
        var definition = new AdHocInterResultDefinition<TKey, TValue>(
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
