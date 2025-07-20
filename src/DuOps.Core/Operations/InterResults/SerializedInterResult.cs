using DuOps.Core.Operations.InterResults.Definitions;

namespace DuOps.Core.Operations.InterResults;

public readonly record struct SerializedInterResult(
    InterResultDiscriminator Discriminator,
    SerializedInterResultKey? Key,
    SerializedInterResultValue Value
);
