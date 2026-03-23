using DuOps.Core.DependencyInjection;
using DuOps.Core.Storages;
using DuOps.Npgsql;
using DuOps.Samples.WebApi.SampleOperation;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddOpenTelemetry().WithMetrics(builder => builder.AddPrometheusExporter());

builder.Services.AddSingleton(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    return new NpgsqlDataSourceBuilder(connectionString).Build();
});

builder.Services.AddDuOps(duOpsBuilder =>
{
    duOpsBuilder.AddOperation(
        SampleOperationHandler.Definition,
        operationBuilder =>
        {
            operationBuilder.AddScopedHandler<SampleOperationHandler>();
            operationBuilder.RetryPolicy = SampleOperationHandler.RetryPolicy;
        }
    );

    duOpsBuilder.AddNpgsqlOperationStorage(
        "StorageName",
        storageBuilder =>
        {
            storageBuilder.UseNpgsqlDataSource();

            storageBuilder
                .OptionsBuilder.BindConfiguration($"DuOps:{storageBuilder.StorageName}")
                .Configure(options =>
                {
                    options.LockDuration = TimeSpan.FromSeconds(30);
                    options.LockExtendingInterval = TimeSpan.FromSeconds(1);
                });

            storageBuilder.AddWorkers("QueueName", 10);
        }
    );
});

var app = builder.Build();

app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "v1");
});

app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.MapPost(
    "/api/operation/schedule",
    async (
        [FromKeyedServices("StorageName")] IOperationStorage operationStorage,
        CancellationToken cancellationToken
    ) =>
    {
        var operationId = Guid.CreateVersion7();

        await operationStorage.ScheduleOperationAsync(
            SampleOperationHandler.Definition,
            "QueueName",
            operationId,
            new SampleOperationArgs(),
            cancellationToken
        );

        return operationId;
    }
);

app.MapPost(
    "/api/operations/{id}/poll",
    async (
        [FromKeyedServices("StorageName")] IOperationStorage operationStorage,
        [FromRoute] Guid id,
        CancellationToken cancellationToken
    ) =>
    {
        var operation = await operationStorage.GetByIdOrDefaultAsync(
            SampleOperationHandler.Definition,
            id,
            cancellationToken
        );

        return operation?.State;
    }
);

app.Run();
