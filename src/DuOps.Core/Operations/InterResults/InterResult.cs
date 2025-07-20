using DuOps.Core.Operations.InterResults.Definitions;

namespace DuOps.Core.Operations.InterResults;

public readonly record struct InterResult<TValue>(
    InterResultDiscriminator Discriminator,
    TValue Value
);

public readonly record struct InterResult<TKey, TValue>(
    InterResultDiscriminator Discriminator,
    TKey Key,
    TValue Value
);
