using DuOps.Core.PollingSchedule;
using Microsoft.Extensions.DependencyInjection;

namespace DuOps.Hangfire;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHangfireOperationPollingScheduler(
        this IServiceCollection services
    )
    {
        return services.AddSingleton<
            IOperationPollingScheduler,
            HangfireOperationPollingScheduler
        >();
    }
}
