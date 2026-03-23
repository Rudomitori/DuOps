using DuOps.Core;
using DuOps.Core.OperationDefinitions;
using DuOps.Core.OperationDefinitions.RetryPolicies;
using DuOps.Core.Serializers;
using DuOps.Samples.WebApi.SampleOperation.InnerResults;

namespace DuOps.Samples.WebApi.SampleOperation;

public sealed class SampleOperationHandler
    : IOperationHandler<Guid, SampleOperationArgs, SampleOperationResult>
{
    public static readonly OperationDefinition<
        Guid,
        SampleOperationArgs,
        SampleOperationResult
    > Definition = new(
        new OperationType("SampleOperation"),
        GuidSerializer.Instance,
        JsonSerializer<SampleOperationArgs>.Default,
        JsonSerializer<SampleOperationResult>.Default
    );

    public static readonly OperationRetryPolicy RetryPolicy = new(
        shouldRetry: (_, retryCount) => retryCount < 3,
        retryDelay: (_, retryCount) =>
            TimeSpan.FromSeconds(retryCount / 2.0 + Random.Shared.NextDouble())
    );

    public async Task<SampleOperationResult> Execute(
        Guid operationId,
        SampleOperationArgs args,
        IOperationExecutionContext context
    )
    {
        await context.RunWithCache(
            RandomSeedInnerResultDefinition.Instance,
            () => Task.FromResult(Random.Shared.Next())
        );

        await context.RunWithCache(
            AwaitExternalLongProcessInnerResultDefinition.Instance,
            async () =>
            {
                var externalProcessIsFinished = Random.Shared.Next() % 2 == 0;
                if (externalProcessIsFinished)
                {
                    return DateTime.UtcNow;
                }

                await context.Wait("WaitExternalLongProcess", TimeSpan.FromSeconds(1));
                return default;
            }
        );

        await context.RunWithCache(
            new("RandomException"),
            () =>
            {
                var shouldThrow = Random.Shared.Next() % 3 != 0;
                return shouldThrow
                    ? Task.FromException(new Exception("Random exception"))
                    : Task.CompletedTask;
            }
        );

        return new SampleOperationResult();
    }
}
