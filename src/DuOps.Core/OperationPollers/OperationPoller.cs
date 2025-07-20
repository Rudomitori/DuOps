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

        if (serializedOperation.State is SerializedOperationState.Finished finished)
        {
            return finished;
        }

        return await registry.InvokeCallbackWithDefinition<
            TypedPollOperationCallbackProxy,
            SerializedOperationState
        >(
            operationDiscriminator,
            new TypedPollOperationCallbackProxy(this, serializedOperation, yieldToken)
        );
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

        if (serializedOperation.State is SerializedOperationState.Finished finished)
        {
            return operationDefinition.Deserialize(finished);
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

        SerializedOperationResult serializedResult;
        TResult result;
        try
        {
            var args = operationDefinition.DeserializeArgsAndWrapException(
                serializedOperation.Args
            );

            result = await operationImplementation.Execute(args, context);

            serializedResult = operationDefinition.SerializeResultAndWrapException(result);
            result = operationDefinition.DeserializeResultAndWrapException(serializedResult);
        }
        catch (Exception e)
        {
            telemetry.OnOperationThrewException(operationDefinition, operationId, e);
            throw;
        }

        telemetry.OnOperationFinished(operationDefinition, operationId, serializedResult);

        await storage.AddResult(
            operationDefinition.Discriminator,
            operationId,
            serializedResult,
            CancellationToken.None
        );

        return new OperationState<TResult>.Finished(result);
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
                .Select(x => new InterResult<TKey, TValue>(
                    x.Key.Discriminator,
                    // TODO: Add null check
                    definition.DeserializeKeyAndWrapException(x.Key.Key.Value),
                    definition.DeserializeValueAndWrapException(x.Value)
                ))
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

        #endregion

        public OperationId OperationId => operationId;

        public CancellationToken YieldToken => yieldToken;
    }
}
