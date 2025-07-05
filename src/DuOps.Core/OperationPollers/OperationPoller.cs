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

        var serializedInterResults = await storage.GetInterResults(
            operationDefinition.Discriminator,
            operationId,
            yieldToken
        );

        var operationImplementation = serviceProvider.GetRequiredService<
            IOperationImplementation<TArgs, TResult>
        >();

        var context = new OperationExecutionContext(
            operationDefinition,
            operationId,
            serializedInterResults.ToDictionary(x => x.Key, x => x.Value),
            storage,
            telemetry,
            yieldToken
        );

        SerializedOperationResult serializedResult;
        OperationResult<TResult> result;
        try
        {
            var args = operationDefinition.Deserialize(serializedOperation.Args);

            var resultValue = await operationImplementation.Execute(args.Value, context);

            result = new OperationResult<TResult>(resultValue);

            serializedResult = operationDefinition.Serialize(result);
            result = operationDefinition.Deserialize(serializedResult);
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
            SerializedInterResult
        > serializedInterResults,
        IOperationStorage storage,
        IOperationTelemetry telemetry,
        CancellationToken yieldToken
    ) : IOperationExecutionContext
    {
        #region AddInterResult

        public async Task AddInterResult<TResult>(
            IInterResultDefinition<TResult> resultDefinition,
            TResult value
        )
        {
            var serializedResult = resultDefinition.Serialize(new InterResult<TResult>(value));

            await storage.AddInterResult(
                operationDefinition.Discriminator,
                operationId,
                resultDefinition.Discriminator,
                key: null,
                serializedResult,
                CancellationToken.None
            );

            serializedInterResults[(resultDefinition.Discriminator, null)] = serializedResult;
        }

        #endregion

        #region GetInterResult

        public InterResult<TResult>? GetInterResultOrNull<TResult>(
            IInterResultDefinition<TResult> definition
        )
        {
            var serializedInterResult = serializedInterResults.GetValueOrDefault(
                (definition.Discriminator, null)
            );

            if (serializedInterResult == default)
            {
                return null;
            }

            return definition.Deserialize(serializedInterResult);
        }

        public InterResult<TResult>? GetInterResultOrNull<TResult, TKey>(
            IKeyedInterResultDefinition<TResult, TKey> definition,
            TKey key
        )
        {
            var serializedKey = definition.Serialize(new InterResultKey<TKey>(key));

            var serializedInterResult = serializedInterResults.GetValueOrDefault(
                (definition.Discriminator, serializedKey)
            );

            if (serializedInterResult == default)
            {
                return null;
            }

            return definition.Deserialize(serializedInterResult);
        }

        public IReadOnlyCollection<
            KeyValuePair<InterResultKey<TKey>, InterResult<TResult>>
        > GetInterResults<TResult, TKey>(IKeyedInterResultDefinition<TResult, TKey> definition)
        {
            return serializedInterResults
                .Where(x => x.Key.Discriminator == definition.Discriminator)
                .Where(x => x.Key.Key is not null)
                .Select(x =>
                    KeyValuePair.Create(
                        definition.Deserialize(x.Key.Key!.Value),
                        definition.Deserialize(x.Value)
                    )
                )
                .ToArray();
        }

        #endregion

        #region RunWithCache

        public async Task<TResult> RunWithCache<TResult>(
            IInterResultDefinition<TResult> definition,
            Func<Task<TResult>> action
        )
        {
            InterResult<TResult> interResult;
            SerializedInterResult serializedInterResult;
            try
            {
                serializedInterResult = serializedInterResults.GetValueOrDefault(
                    (definition.Discriminator, null)
                );

                if (serializedInterResult != default)
                    return definition.Deserialize(serializedInterResult).Value;

                var interResultValue = await action();
                interResult = new InterResult<TResult>(interResultValue);

                serializedInterResult = definition.Serialize(interResult);
                interResult = definition.Deserialize(serializedInterResult);
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

            await storage.AddInterResult(
                operationDefinition.Discriminator,
                operationId,
                definition.Discriminator,
                key: null,
                serializedInterResult,
                CancellationToken.None
            );

            serializedInterResults[(definition.Discriminator, null)] = serializedInterResult;

            telemetry.OnInterResultAdded(
                operationDefinition,
                operationId,
                definition,
                null,
                serializedInterResult
            );

            return interResult.Value;
        }

        public async Task<TResult> RunWithCache<TResult, TKey>(
            IKeyedInterResultDefinition<TResult, TKey> definition,
            TKey keyValue,
            Func<Task<TResult>> action
        )
        {
            var interResultKey = new InterResultKey<TKey>(keyValue);
            SerializedInterResultKey serializedInterResultKey;

            InterResult<TResult> interResult;
            SerializedInterResult serializedInterResult;
            try
            {
                serializedInterResultKey = definition.Serialize(interResultKey);

                serializedInterResult = serializedInterResults.GetValueOrDefault(
                    (definition.Discriminator, serializedInterResultKey)
                );

                if (serializedInterResult != default)
                    return definition.Deserialize(serializedInterResult).Value;

                var interResultValue = await action();
                interResult = new InterResult<TResult>(interResultValue);

                serializedInterResult = definition.Serialize(interResult);
                interResult = definition.Deserialize(serializedInterResult);
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

            await storage.AddInterResult(
                operationDefinition.Discriminator,
                operationId,
                definition.Discriminator,
                key: serializedInterResultKey,
                serializedInterResult,
                CancellationToken.None
            );

            serializedInterResults[(definition.Discriminator, serializedInterResultKey)] =
                serializedInterResult;

            telemetry.OnInterResultAdded(
                operationDefinition,
                operationId,
                definition,
                serializedInterResultKey,
                serializedInterResult
            );

            return interResult.Value;
        }

        #endregion

        public OperationId OperationId => operationId;

        public CancellationToken YieldToken => yieldToken;
    }
}
