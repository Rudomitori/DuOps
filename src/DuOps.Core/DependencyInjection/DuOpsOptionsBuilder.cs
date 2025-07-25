using Microsoft.Extensions.DependencyInjection;

namespace DuOps.Core.DependencyInjection;

public readonly record struct DuOpsOptionsBuilder(IServiceCollection Services);
