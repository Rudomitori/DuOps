using DuOps.Core.Exceptions;
using DuOps.Core.InnerResults;
using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;
using DuOps.Core.Registry;
using DuOps.Core.Storages;
using DuOps.Core.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DuOps.Core.Workers;

internal sealed class Worker(
    IOperationStorage storage,
    ILogger<Worker> logger,
    OperationRegistry registry,
    IOperationTelemetry telemetry,
    TimeProvider timeProvider,
    IServiceProvider serviceProvider,
    string queue
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var enumerableHandles = storage.EnumerateForExecutionAsync(queue, stoppingToken);
                await foreach (var storageHandle in enumerableHandles)
                    await using (storageHandle)
                        await ExecuteAsync(storageHandle, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Exception occured while executing {Queue}", queue);
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }

    private async Task ExecuteAsync(
        IOperationStorageHandle storageHandle,
        CancellationToken cancellationToken = default
    )
    {
        await registry.InvokeCallbackWithEntry<TypedExecuteOperationDefinitionCallbackProxy, Task>(
            storageHandle.OperationType,
            new TypedExecuteOperationDefinitionCallbackProxy(this, storageHandle, cancellationToken)
        );
    }

    private async Task ExecuteAsync<TId, TArgs, TResult>(
        OperationRegistryEntry<TId, TArgs, TResult> registryEntry,
        IOperationStorageHandle storageHandle,
        CancellationToken yieldToken = default
    )
    {
        var operationDefinition = registryEntry.OperationDefinition;
        var serializedOperationId = storageHandle.SerializedOperationId;

        var executionContext = new OperationExecutionContext(
            operationDefinition,
            storageHandle,
            telemetry,
            timeProvider,
            yieldToken
        );

        try
        {
            using var serviceScope = serviceProvider.CreateScope();
            // TODO: Handle case when handler is not registered
            var operationHandler = registryEntry.OperationHandlerFactory!(
                serviceScope.ServiceProvider
            );

            var operationId = operationDefinition.DeserializeId(serializedOperationId);
            var args = operationDefinition.DeserializeArgs(storageHandle.OperationArgs);

            var result = await operationHandler.Execute(operationId, args, executionContext);

            var serializedResult = operationDefinition.SerializeResult(result);

            await storageHandle.CompleteAsync(serializedResult, CancellationToken.None);

            telemetry.OnOperationFinished(
                operationDefinition,
                serializedOperationId,
                serializedResult
            );
        }
        // TODO: Handle AggregateException
        catch (OperationCanceledException) when (yieldToken.IsCancellationRequested)
        {
            telemetry.OnOperationYielded(operationDefinition, serializedOperationId);
        }
        catch (WaitException e)
        {
            var waitingUntil = e switch
            {
                { Duration: { } duration } => timeProvider.GetUtcNow().DateTime + duration,
                { Until: { } until } => until,
                _ => throw new Exception("Unexpected waiting state", e),
            };

            telemetry.OnOperationWaiting(
                operationDefinition,
                serializedOperationId,
                waitingUntil,
                e.Reason
            );

            await storageHandle.RescheduleAsync(waitingUntil, CancellationToken.None);
        }
        catch (Exception e)
        {
            var retryCount = storageHandle.RetryCount;
            var retryPolicy = registry.GetRetryPolicyOrDefault(storageHandle.OperationType);

            if (retryPolicy is not null && retryPolicy.ShouldRetry(e, retryCount))
            {
                var retryDelay = retryPolicy.RetryDelay(e, retryCount);
                var retryingAt = timeProvider.GetUtcNow().DateTime + retryDelay;

                await storageHandle.ScheduleRetryAsync(retryingAt, CancellationToken.None);

                telemetry.OnOperationThrewException(
                    operationDefinition,
                    serializedOperationId,
                    e,
                    retryingAt
                );
            }
            else
            {
                await storageHandle.FailAsync(e.Message, CancellationToken.None);
                telemetry.OnOperationFailed(operationDefinition, serializedOperationId, e);
            }
        }
    }

    private readonly struct TypedExecuteOperationDefinitionCallbackProxy(
        Worker worker,
        IOperationStorageHandle storageHandle,
        CancellationToken yieldToken
    ) : IOperationRegistryCallback<Task>
    {
        public Task Invoke<TId, TArgs, TResult>(
            OperationRegistryEntry<TId, TArgs, TResult> registryEntry
        )
        {
            return worker.ExecuteAsync(registryEntry, storageHandle, yieldToken);
        }
    }

    private sealed class OperationExecutionContext(
        IOperationDefinition operationDefinition,
        IOperationStorageHandle storageHandle,
        IOperationTelemetry telemetry,
        TimeProvider timeProvider,
        CancellationToken yieldToken
    ) : IOperationExecutionContext
    {
        public async Task AddInnerResult<TValue>(
            IInnerResultDefinition<TValue> resultDefinition,
            TValue value,
            CancellationToken cancellationToken = default
        )
        {
            var serializedValue = resultDefinition.SerializeValue(value);

            var serializedInnerResult = new SerializedInnerResult(
                Type: resultDefinition.Type,
                Id: null,
                Value: serializedValue,
                CreatedAt: timeProvider.GetUtcNow().UtcDateTime,
                UpdatedAt: null
            );

            storageHandle.AddInnerResultLazy(serializedInnerResult);
            await storageHandle.FlushInnerResultsAsync();
        }

        public InnerResult<TValue>? GetInnerResultOrNull<TValue>(
            IInnerResultDefinition<TValue> definition
        )
        {
            var serializedInnerResult = storageHandle.GetInnerResultOrDefault(
                definition.Type,
                id: null
            );

            if (serializedInnerResult is null)
            {
                return null;
            }

            var value = definition.DeserializeValue(serializedInnerResult.Value);

            return new InnerResult<TValue>(
                definition.Type,
                value,
                CreatedAt: serializedInnerResult.CreatedAt,
                UpdatedAt: serializedInnerResult.UpdatedAt
            );
        }

        public InnerResult<TId, TValue>? GetInnerResultOrNull<TId, TValue>(
            IInnerResultDefinition<TId, TValue> definition,
            TId id
        )
        {
            var serializedId = definition.SerializeId(id);

            var serializedInnerResult = storageHandle.GetInnerResultOrDefault(
                definition.Type,
                serializedId
            );

            if (serializedInnerResult is null)
            {
                return null;
            }

            var value = definition.DeserializeValue(serializedInnerResult.Value);

            return new InnerResult<TId, TValue>(
                definition.Type,
                id,
                value,
                CreatedAt: serializedInnerResult.CreatedAt,
                UpdatedAt: serializedInnerResult.UpdatedAt
            );
        }

        public InnerResult<TId, TValue>[] GetInnerResults<TId, TValue>(
            IInnerResultDefinition<TId, TValue> definition
        )
        {
            return storageHandle
                .GetInnerResults(definition.Type)
                .Select(definition.Deserialize)
                .ToArray();
        }

        public async Task<TValue> RunWithCache<TValue>(
            IInnerResultDefinition<TValue> innerResultDefinition,
            Func<Task<TValue>> action
        )
        {
            TValue value;
            SerializedInnerResultValue serializedValue;
            SerializedInnerResult? serializedInnerResult;
            try
            {
                serializedInnerResult = storageHandle.GetInnerResultOrDefault(
                    innerResultDefinition.Type,
                    id: null
                );

                if (serializedInnerResult is not null)
                    return innerResultDefinition.DeserializeValue(serializedInnerResult.Value);

                value = await action();

                serializedValue = innerResultDefinition.SerializeValue(value);
                value = innerResultDefinition.DeserializeValue(serializedValue);
            }
            catch (OperationCanceledException) when (YieldToken.IsCancellationRequested)
            {
                throw;
            }
            catch (WaitException)
            {
                throw;
            }
            catch (Exception e)
            {
                telemetry.OnInnerResultThrewException(
                    operationDefinition,
                    storageHandle.SerializedOperationId,
                    innerResultDefinition,
                    innerResultKey: null,
                    e
                );
                throw;
            }

            serializedInnerResult = new SerializedInnerResult(
                innerResultDefinition.Type,
                Id: null,
                serializedValue,
                CreatedAt: timeProvider.GetUtcNow().UtcDateTime,
                UpdatedAt: null
            );

            storageHandle.AddInnerResultLazy(serializedInnerResult);
            await storageHandle.FlushInnerResultsAsync();

            telemetry.OnInnerResultAdded(
                operationDefinition,
                storageHandle.SerializedOperationId,
                serializedInnerResult
            );

            return value;
        }

        public async Task<TValue> RunWithCache<TKey, TValue>(
            IInnerResultDefinition<TKey, TValue> definition,
            TKey id,
            Func<Task<TValue>> action
        )
        {
            SerializedInnerResultId serializedId;

            TValue value;
            SerializedInnerResultValue serializedValue;
            try
            {
                serializedId = definition.SerializeId(id);

                var serializedInnerResult = storageHandle.GetInnerResultOrDefault(
                    definition.Type,
                    serializedId
                );

                if (serializedInnerResult is not null)
                    return definition.DeserializeValue(serializedInnerResult.Value);

                value = await action();

                serializedValue = definition.SerializeValue(value);
                value = definition.DeserializeValue(serializedValue);
            }
            catch (OperationCanceledException) when (YieldToken.IsCancellationRequested)
            {
                throw;
            }
            catch (WaitException)
            {
                throw;
            }
            catch (Exception e)
            {
                telemetry.OnInnerResultThrewException(
                    operationDefinition,
                    storageHandle.SerializedOperationId,
                    definition,
                    innerResultKey: null,
                    e
                );
                throw;
            }

            var innerResult = new SerializedInnerResult(
                definition.Type,
                serializedId,
                serializedValue,
                CreatedAt: timeProvider.GetUtcNow().UtcDateTime,
                UpdatedAt: null
            );

            storageHandle.AddInnerResultLazy(innerResult);
            await storageHandle.FlushInnerResultsAsync();

            telemetry.OnInnerResultAdded(
                operationDefinition,
                storageHandle.SerializedOperationId,
                innerResult
            );

            return value;
        }

        public Task Wait(string reason, TimeSpan duration)
        {
            return Task.FromException(new WaitException(reason, duration));
        }

        public Task Wait(string reason, DateTime until)
        {
            if (until.Kind != DateTimeKind.Utc)
                throw new ArgumentException("until must have Kind == Utc", nameof(until));

            var now = timeProvider.GetUtcNow().UtcDateTime;
            if (until < now)
                return Task.CompletedTask;

            return Task.FromException(new WaitException(reason, until));
        }

        public SerializedOperationId SerializedOperationId => storageHandle.SerializedOperationId;
        public CancellationToken YieldToken => yieldToken;
    }
}
