using DuOps.Core.PollingSchedule;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DuOps.InMemory;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInMemoryBackgroundOperationPollScheduler(
        this IServiceCollection services
    )
    {
        services.TryAddSingleton<InMemoryBackgroundOperationPollScheduler>();
        services.TryAddSingleton<IOperationPollingScheduler>(provider =>
            provider.GetRequiredService<InMemoryBackgroundOperationPollScheduler>()
        );
        services.AddHostedService<InMemoryBackgroundOperationPollScheduler>(provider =>
            provider.GetRequiredService<InMemoryBackgroundOperationPollScheduler>()
        );

        return services;
    }
}
