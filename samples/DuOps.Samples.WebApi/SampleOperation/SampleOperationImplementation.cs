using DuOps.Core;
using DuOps.Core.OperationPollers;
using DuOps.Samples.WebApi.SampleOperation.InterResults;

namespace DuOps.Samples.WebApi.SampleOperation;

public sealed class SampleOperationImplementation
    : IOperationImplementation<SampleOperationArgs, SampleOperationResult>
{
    public async Task<SampleOperationResult> Execute(
        SampleOperationArgs args,
        IOperationExecutionContext context
    )
    {
        await context.RunWithCache(
            RandomSeedInterResultDefinition.Instance,
            () => Task.FromResult(Random.Shared.Next())
        );

        return new SampleOperationResult();
    }
}
