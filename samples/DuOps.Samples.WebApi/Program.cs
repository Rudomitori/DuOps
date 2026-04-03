using DuOps.Core;
using DuOps.Core.Client;
using DuOps.Core.DependencyInjection;
using DuOps.Core.Storages;
using DuOps.Npgsql;
using DuOps.OpenTelemetry;
using DuOps.Samples.WebApi.SampleOperation;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using OpenTelemetry.Metrics;

var appOperationQueueId = new OperationQueueId("Default");
var appOperationStorageId = new OperationStorageId("Default");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder
    .Services.AddOpenTelemetry()
    .WithMetrics(static builder => builder.AddDuOpsInstrumentation().AddPrometheusExporter());

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
        appOperationStorageId,
        storageBuilder =>
        {
            storageBuilder.UseNpgsqlDataSource();

            storageBuilder
                .OptionsBuilder.BindConfiguration($"DuOps:{storageBuilder.StorageId}")
                .Configure(options =>
                {
                    options.LockDuration = TimeSpan.FromSeconds(30);
                    options.LockExtendingInterval = TimeSpan.FromSeconds(1);
                });

            storageBuilder.AddWorkers(appOperationQueueId, 10);
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
    async (IDuOpsClient duOpsClient, CancellationToken cancellationToken) =>
    {
        var operationId = Guid.CreateVersion7();

        await duOpsClient.ScheduleOperationAsync(
            SampleOperationHandler.Definition,
            appOperationStorageId,
            appOperationQueueId,
            operationId,
            new SampleOperationArgs(),
            cancellationToken
        );

        return operationId;
    }
);

app.MapPost(
    "/api/operations/{id}/poll",
    async (IDuOpsClient duOpsClient, [FromRoute] Guid id, CancellationToken cancellationToken) =>
    {
        var operation = await duOpsClient.GetOperationByIdOrDefaultAsync(
            SampleOperationHandler.Definition,
            appOperationStorageId,
            id,
            cancellationToken
        );

        return operation?.State;
    }
);

app.Run();
