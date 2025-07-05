using DuOps.Core.Operations.InterResults.Definitions;

namespace DuOps.Core.Operations.InterResults;

public sealed record SerializedInterResult(
    InterResultDiscriminator Discriminator,
    string? Key,
    string Value
);
