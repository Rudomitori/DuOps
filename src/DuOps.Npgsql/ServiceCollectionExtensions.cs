using DuOps.Core.Storages;
using Microsoft.Extensions.DependencyInjection;

namespace DuOps.Npgsql;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNpgsqlOperationStorage(this IServiceCollection services)
    {
        services.AddSingleton<IOperationStorage, NpgsqlOperationStorage>();

        return services;
    }
}
