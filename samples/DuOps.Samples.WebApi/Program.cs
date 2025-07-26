using DuOps.Core.DependencyInjection;
using DuOps.Core.OperationDefinitions;
using DuOps.Core.OperationManagers;
using DuOps.Core.OperationPollers;
using DuOps.Core.Operations;
using DuOps.Hangfire;
using DuOps.Npgsql;
using DuOps.OpenTelemetry;
using DuOps.Samples.WebApi.SampleOperation;
using Hangfire;
using Hangfire.MemoryStorage;
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

builder.Services.AddDuOps(builder =>
{
    builder.AddOpenTelemetryInstrumentation();
    builder.Services.AddNpgsqlOperationStorage();
    builder.Services.AddHangfireOperationPollingScheduler();

    builder.Services.AddDuOpsOperation<
        SampleOperationArgs,
        SampleOperationResult,
        SampleOperationImplementation
    >(SampleOperationDefinition.Instance);
});

builder.Services.AddHangfire(config => config.UseMemoryStorage());
builder.Services.AddHangfireServer(config =>
{
    config.SchedulePollingInterval = TimeSpan.FromMilliseconds(200);
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
    async ([FromServices] IOperationManager operationManager) =>
    {
        var operation = SampleOperationDefinition.Instance.NewOperation(
            OperationId.NewGuid(),
            new SampleOperationArgs(),
            DateTime.UtcNow
        );
        operation = await operationManager.StartInBackground(
            SampleOperationDefinition.Instance,
            operation
        );

        return operation.Id;
    }
);

app.MapPost(
    "/api/operations/{id}/poll",
    async ([FromServices] IOperationPoller poller, [FromRoute] string id) =>
    {
        var operationState = await poller.PollOperation(
            SampleOperationDefinition.Instance,
            new OperationId(null, id)
        );
        return operationState;
    }
);

app.Run();
