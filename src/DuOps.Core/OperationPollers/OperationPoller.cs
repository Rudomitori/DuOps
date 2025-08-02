using DuOps.Core.Exceptions;
using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;
using DuOps.Core.Operations.InterResults;
using DuOps.Core.Operations.InterResults.Definitions;
using DuOps.Core.Registry;
using DuOps.Core.Storages;
using DuOps.Core.Telemetry;
using Microsoft.Extensions.DependencyInjection;

namespace DuOps.Core.OperationPollers;

internal sealed class OperationPoller(
    IOperationStorage storage,
    IOperationDefinitionRegistry registry,
    IServiceProvider serviceProvider,
    IOperationTelemetry telemetry
) : IOperationPoller
{
    public async Task<SerializedOperationState> PollOperation(
        OperationDiscriminator operationDiscriminator,
        OperationId operationId,
        CancellationToken yieldToken = default
    )
    {
        var serializedOperation = await storage.GetByIdOrDefault(
            operationDiscriminator,
            operationId,
            yieldToken
        );

        if (serializedOperation is null)
            throw new InvalidOperationException($"Operation {operationId} was not found");

        return await PollOperation(serializedOperation, yieldToken);
    }

    public async Task<SerializedOperationState> PollOperation(
        SerializedOperation serializedOperation,
        CancellationToken yieldToken = default
    )
    {
        return serializedOperation.State switch
        {
            SerializedOperationState.Finished or SerializedOperationState.Failed =>
                serializedOperation.State,
            _ => await registry.InvokeCallbackWithDefinition<
                TypedPollOperationCallbackProxy,
                SerializedOperationState
            >(
                serializedOperation.Discriminator,
                new TypedPollOperationCallbackProxy(this, serializedOperation, yieldToken)
            ),
        };
    }

    private readonly struct TypedPollOperationCallbackProxy(
        OperationPoller poller,
        SerializedOperation operation,
        CancellationToken yieldToken
    ) : IOperationDefinitionGenericCallback<SerializedOperationState>
    {
        public async Task<SerializedOperationState> Invoke<TOperationArgs, TOperationResult>(
            IOperationDefinition<TOperationArgs, TOperationResult> operationDefinition
        )
        {
            var operationState = await poller.PollOperation(
                operationDefinition,
                operation,
                yieldToken
            );
            return operationDefinition.Serialize(operationState);
        }
    }

    public async Task<OperationState<TResult>> PollOperation<TArgs, TResult>(
        IOperationDefinition<TArgs, TResult> operationDefinition,
        OperationId operationId,
        CancellationToken yieldToken = default
    )
    {
        var serializedOperation = await storage.GetByIdOrDefault(
            operationDefinition.Discriminator,
            operationId,
            yieldToken
        );

        if (serializedOperation is null)
            throw new InvalidOperationException($"Operation {operationId} was not found");

        return await PollOperation(operationDefinition, serializedOperation, yieldToken);
    }

    public async Task<OperationState<TResult>> PollOperation<TArgs, TResult>(
        IOperationDefinition<TArgs, TResult> operationDefinition,
        SerializedOperation serializedOperation,
        CancellationToken yieldToken
    )
    {
        var operationId = serializedOperation.Id;

        if (
            serializedOperation.State
            is SerializedOperationState.Finished
                or SerializedOperationState.Failed
        )
        {
            return operationDefinition.Deserialize(serializedOperation.State);
        }

        var operationImplementation = serviceProvider.GetRequiredService<
            IOperationImplementation<TArgs, TResult>
        >();

        var context = new OperationExecutionContext(
            operationDefinition,
            operationId,
            serializedOperation.InterResults.ToDictionary(
                x => (x.Discriminator, x.Key),
                x => x.Value
            ),
            storage,
            telemetry,
            yieldToken
        );

        OperationState<TResult> newState;
        try
        {
            var args = operationDefinition.DeserializeArgsAndWrapException(
                serializedOperation.Args
            );

            var result = await operationImplementation.Execute(args, context);

            var serializedResult = operationDefinition.SerializeResultAndWrapException(result);
            result = operationDefinition.DeserializeResultAndWrapException(serializedResult);

            newState = new OperationState<TResult>.Finished(result);

            telemetry.OnOperationFinished(operationDefinition, operationId, serializedResult);
        }
        // TODO: Handle AggregateException
        catch (OperationCanceledException) when (yieldToken.IsCancellationRequested)
        {
            telemetry.OnOperationYielded(operationDefinition, operationId);
            newState = OperationState<TResult>.Yielded.Instance;
        }
        catch (WaitException e)
        {
            var waitingUntil = e switch
            {
                // TODO: Use TimeProvider to get now
                { Duration: { } duration } => DateTimeOffset.UtcNow + duration,
                { Until: { } until } => until,
                _ => throw new Exception("Unexpected waiting state", e),
            };

            telemetry.OnOperationWaiting(operationDefinition, operationId, waitingUntil, e.Reason);

            newState = new OperationState<TResult>.Waiting(waitingUntil);
        }
        catch (Exception e)
        {
            var retryCount = serializedOperation.State is SerializedOperationState.Retrying retrying
                ? retrying.RetryCount
                : 0;

            if (operationDefinition.RetryPolicy.ShouldRetry(e, retryCount))
            {
                var retryDelay = operationDefinition.RetryPolicy.RetryDelay(e, retryCount);
                var retryingAt = DateTimeOffset.UtcNow + retryDelay;

                newState = new OperationState<TResult>.Retrying(retryingAt, retryCount + 1);

                telemetry.OnOperationThrewException(
                    operationDefinition,
                    operationId,
                    e,
                    retryingAt
                );
            }
            else
            {
                newState = new OperationState<TResult>.Failed(e.Message);
                telemetry.OnOperationFailed(operationDefinition, operationId, e);
            }
        }

        await storage.SetState(
            operationDefinition.Discriminator,
            operationId,
            operationDefinition.Serialize(newState),
            CancellationToken.None
        );

        return newState;
    }

    private sealed class OperationExecutionContext(
        IOperationDefinition operationDefinition,
        OperationId operationId,
        Dictionary<
            (InterResultDiscriminator Discriminator, SerializedInterResultKey? Key),
            SerializedInterResultValue
        > serializedInterResults,
        IOperationStorage storage,
        IOperationTelemetry telemetry,
        CancellationToken yieldToken
    ) : IOperationExecutionContext
    {
        #region AddInterResult

        public async Task AddInterResult<T>(
            IInterResultDefinition<T> resultDefinition,
            T value,
            CancellationToken cancellationToken = default
        )
        {
            var serializedValue = resultDefinition.SerializeValueAndWrapException(value);

            var serializedInterResult = new SerializedInterResult(
                resultDefinition.Discriminator,
                Key: null,
                serializedValue
            );

            await storage.AddInterResult(
                operationDefinition.Discriminator,
                operationId,
                serializedInterResult,
                cancellationToken
            );

            serializedInterResults[(resultDefinition.Discriminator, null)] = serializedValue;
        }

        #endregion

        #region GetInterResult

        public InterResult<T>? GetInterResultOrNull<T>(IInterResultDefinition<T> definition)
        {
            var serializedValue = serializedInterResults.GetValueOrDefault(
                (definition.Discriminator, null)
            );

            if (serializedValue == default)
            {
                return null;
            }

            var value = definition.DeserializeValueAndWrapException(serializedValue);

            return new InterResult<T>(definition.Discriminator, value);
        }

        public InterResult<TKey, TValue>? GetInterResultOrNull<TKey, TValue>(
            IInterResultDefinition<TKey, TValue> definition,
            TKey key
        )
        {
            var serializedKey = definition.SerializeKeyAndWrapException(key);

            var serializedValue = serializedInterResults.GetValueOrDefault(
                (definition.Discriminator, serializedKey)
            );

            if (serializedValue == default)
            {
                return null;
            }

            var value = definition.DeserializeValueAndWrapException(serializedValue);

            return new InterResult<TKey, TValue>(definition.Discriminator, key, value);
        }

        public IReadOnlyCollection<InterResult<TKey, TValue>> GetInterResults<TKey, TValue>(
            IInterResultDefinition<TKey, TValue> definition
        )
        {
            return serializedInterResults
                .Where(x => x.Key.Discriminator == definition.Discriminator)
                .Select(x =>
                {
                    if (x.Key.Key is null)
                    {
                        throw new InvalidOperationException(
                            "Not keyed inter result can not be read as keyed inter result"
                        );
                    }

                    return new InterResult<TKey, TValue>(
                        x.Key.Discriminator,
                        definition.DeserializeKeyAndWrapException(x.Key.Key.Value),
                        definition.DeserializeValueAndWrapException(x.Value)
                    );
                })
                .ToArray();
        }

        #endregion

        #region RunWithCache

        public async Task<TValue> RunWithCache<TValue>(
            IInterResultDefinition<TValue> definition,
            Func<Task<TValue>> action
        )
        {
            TValue value;
            SerializedInterResultValue serializedValue;
            try
            {
                serializedValue = serializedInterResults.GetValueOrDefault(
                    (definition.Discriminator, null)
                );

                if (serializedValue != default)
                    return definition.DeserializeValueAndWrapException(serializedValue);

                value = await action();

                serializedValue = definition.SerializeValueAndWrapException(value);
                value = definition.DeserializeValueAndWrapException(serializedValue);
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
                telemetry.OnInterResultThrewException(
                    operationDefinition,
                    operationId,
                    definition,
                    interResultKey: null,
                    e
                );
                throw;
            }

            var interResult = new SerializedInterResult(
                definition.Discriminator,
                Key: null,
                serializedValue
            );

            await storage.AddInterResult(
                operationDefinition.Discriminator,
                operationId,
                interResult,
                CancellationToken.None
            );

            serializedInterResults[(definition.Discriminator, null)] = serializedValue;

            telemetry.OnInterResultAdded(operationDefinition, operationId, interResult);

            return value;
        }

        public async Task<TValue> RunWithCache<TKey, TValue>(
            IInterResultDefinition<TKey, TValue> definition,
            TKey key,
            Func<Task<TValue>> action
        )
        {
            SerializedInterResultKey serializedKey;

            TValue value;
            SerializedInterResultValue serializedValue;
            try
            {
                serializedKey = definition.SerializeKeyAndWrapException(key);

                serializedValue = serializedInterResults.GetValueOrDefault(
                    (definition.Discriminator, serializedKey)
                );

                if (serializedValue != default)
                    return definition.DeserializeValueAndWrapException(serializedValue);

                value = await action();

                serializedValue = definition.SerializeValueAndWrapException(value);
                value = definition.DeserializeValueAndWrapException(serializedValue);
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
                telemetry.OnInterResultThrewException(
                    operationDefinition,
                    operationId,
                    definition,
                    interResultKey: null,
                    e
                );
                throw;
            }

            var interResult = new SerializedInterResult(
                definition.Discriminator,
                serializedKey,
                serializedValue
            );

            await storage.AddInterResult(
                operationDefinition.Discriminator,
                operationId,
                interResult,
                CancellationToken.None
            );

            serializedInterResults[(definition.Discriminator, serializedKey)] = serializedValue;

            telemetry.OnInterResultAdded(operationDefinition, operationId, interResult);

            return value;
        }

        public Task Wait(string reason, TimeSpan duration)
        {
            return Task.FromException(new WaitException(reason, duration));
        }

        public Task Wait(string reason, DateTimeOffset until)
        {
            return Task.FromException(new WaitException(reason, until));
        }

        #endregion

        public OperationId OperationId => operationId;

        public CancellationToken YieldToken => yieldToken;
    }
}
