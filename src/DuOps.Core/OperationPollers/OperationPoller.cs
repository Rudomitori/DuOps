using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;
using DuOps.Core.Operations.InterResults;
using DuOps.Core.Operations.InterResults.Definitions;
using DuOps.Core.Repositories;
using DuOps.Core.Telemetry;
using Microsoft.Extensions.DependencyInjection;

namespace DuOps.Core.OperationPollers;

internal sealed class OperationPoller(
    IOperationStorage storage,
    IServiceProvider serviceProvider,
    IOperationTelemetry telemetry
) : IOperationPoller
{
    public async Task<OperationExecutionResult<TResult>> PollOperation<TArgs, TResult>(
        IOperationDefinition<TArgs, TResult> operationDefinition,
        SerializedOperation serializedOperation,
        CancellationToken yieldToken
    )
    {
        var operationId = serializedOperation.Id;

        if (serializedOperation.ExecutionResult is OperationExecutionResult<string>.Finished finished)
        {
            return new OperationExecutionResult<TResult>.Finished(
                OperationHelper.DeserializeResult(finished.Result, operationDefinition.DeserializeResult)
            );
        }

        var serializedInterResults = await storage.GetInterResults(
            operationDefinition.Discriminator,
            operationId,
            yieldToken
        );

        var operationImplementation = serviceProvider.GetRequiredService<IOperationImplementation<TArgs, TResult>>();

        var context = new OperationExecutionContext(
            operationDefinition,
            operationId,
            serializedInterResults.ToDictionary(
                x => (x.Discriminator, x.Key),
                x => x.Value
            ),
            storage,
            telemetry,
            yieldToken
        );

        string? serializedResult;
        TResult result;
        try
        {
            var args = OperationHelper.DeserializeArgs(serializedOperation.Args, operationDefinition.DeserializeArgs);

            result = await operationImplementation.Execute(args, context);

            serializedResult = OperationHelper.SerializeResult(
                result,
                operationDefinition.SerializeResult
            );

            result = OperationHelper.DeserializeResult(
                serializedResult,
                operationDefinition.DeserializeResult
            );
        }
        catch (Exception e)
        {
            telemetry.OnOperationThrewException(
                operationDefinition,
                operationId,
                e
            );
            throw;
        }

        telemetry.OnOperationFinished(operationDefinition, operationId, serializedResult);

        await storage.AddResult(
            operationDefinition.Discriminator,
            operationId,
            serializedResult,
            CancellationToken.None
        );

        return new OperationExecutionResult<TResult>.Finished(result);
    }

    private sealed class OperationExecutionContext(
        IOperationDefinition operationDefinition,
        OperationId operationId,
        Dictionary<(InterResultDiscriminator Discriminator, string? Key), string> serializedInterResults,
        IOperationStorage storage,
        IOperationTelemetry telemetry,
        CancellationToken yieldToken
    ) : IOperationExecutionContext
    {
        public async Task AddInterResult<TResult>(
            IInterResultDefinition<TResult> resultDefinition,
            TResult value
        )
        {
            var serializedResultValue = OperationHelper.SerializeInterResultValue(
                resultDefinition.Serialize,
                value
            );

            await storage.AddInterResult(
                operationDefinition.Discriminator,
                operationId,
                new SerializedInterResult(
                    resultDefinition.Discriminator,
                    null,
                    serializedResultValue
                ),
                CancellationToken.None
            );

            serializedInterResults[(resultDefinition.Discriminator, null)] = serializedResultValue;
        }

        public InterResult<TResult>? GetInterResultOrDefault<TResult>(
            IInterResultDefinition<TResult> resultDefinition
        )
        {
            if (serializedInterResults.TryGetValue((resultDefinition.Discriminator, null), out var serializedValue))
            {
                var value = OperationHelper.DeserializeInterResultValue(
                    resultDefinition.Deserialize,
                    serializedValue
                );

                return new InterResult<TResult>(value);
            }

            return null;
        }

        public KeyedInterResult<TResult, TKey>? GetInterResultOrDefault<TResult, TKey>(
            IKeyedInterResultDefinition<TResult, TKey> resultDefinition,
            TKey key
        )
        {
            var serializedKey = resultDefinition.SerializeKey(key);

            if (serializedInterResults.TryGetValue((resultDefinition.Discriminator, serializedKey), out var serializedValue))
            {
                var value = OperationHelper.DeserializeInterResultValue(
                    resultDefinition.Deserialize,
                    serializedValue
                );

                return new KeyedInterResult<TResult, TKey>(key, value);
            }

            return null;
        }

        public IReadOnlyCollection<KeyedInterResult<TResult, TKey>> GetInterResultsOrDefault<TResult, TKey>(
            IKeyedInterResultDefinition<TResult, TKey> resultDefinition
        )
        {
            return serializedInterResults
                   .Where(x => x.Key.Discriminator == resultDefinition.Discriminator)
                   .Select(x => new KeyedInterResult<TResult, TKey>(
                               OperationHelper.DeserializeInterResultKey(resultDefinition.DeserializeKey, x.Key.Key),
                               OperationHelper.DeserializeInterResultValue(resultDefinition.Deserialize, x.Value)
                           )
                   )
                   .ToArray();
        }

        public async Task<TResult> RunWithCache<TResult>(
            IInterResultDefinition<TResult> resultDefinition,
            Func<Task<TResult>> action
        )
        {
            return await RunWithCacheInternal(
                resultDefinition,
                serializedKey: null,
                deserializeResult: resultDefinition.Deserialize,
                serializeResult: resultDefinition.Serialize,
                action
            );
        }

        public async Task<TResult> RunWithCache<TResult, TKey>(
            IKeyedInterResultDefinition<TResult, TKey> resultDefinition,
            TKey key,
            Func<Task<TResult>> action
        )
        {
            var serializeInterResultKey = OperationHelper.SerializeInterResultKey(
                resultDefinition.SerializeKey,
                key
            );

            return await RunWithCacheInternal(
                resultDefinition,
                serializeInterResultKey,
                deserializeResult: resultDefinition.Deserialize,
                serializeResult: resultDefinition.Serialize,
                action
            );
        }

        public OperationId OperationId => operationId;

        public CancellationToken YieldToken => yieldToken;

        private async Task<TResult> RunWithCacheInternal<TResult>(
            IInterResultDefinition resultDefinition,
            string? serializedKey,
            Func<string, TResult> deserializeResult,
            Func<TResult, string> serializeResult,
            Func<Task<TResult>> action
        )
        {
            TResult interResultValue;
            string serializedInterResultValue;
            try
            {
                var existedSerializedInterResultValue = serializedInterResults.GetValueOrDefault(
                    (resultDefinition.Discriminator, serializedKey)
                );

                if (existedSerializedInterResultValue is not null)
                {
                    return OperationHelper.DeserializeInterResultValue(
                        deserializeResult,
                        existedSerializedInterResultValue
                    );
                }

                interResultValue = await action();

                serializedInterResultValue = OperationHelper.SerializeInterResultValue(
                    serializeResult,
                    interResultValue
                );

                interResultValue = OperationHelper.DeserializeInterResultValue(
                    deserializeResult,
                    serializedInterResultValue
                );
            }
            catch (Exception e)
            {
                telemetry.OnInterResultThrewException(
                    operationDefinition,
                    operationId,
                    resultDefinition,
                    interResultKey: null,
                    e
                );
                throw;
            }

            await storage.AddInterResult(
                operationDefinition.Discriminator,
                operationId,
                new SerializedInterResult(
                    resultDefinition.Discriminator,
                    serializedKey,
                    serializedInterResultValue
                ),
                CancellationToken.None
            );

            serializedInterResults[(resultDefinition.Discriminator, serializedKey)] =
                serializedInterResultValue;

            telemetry.OnInterResultAdded(
                operationDefinition,
                operationId,
                resultDefinition,
                serializedKey,
                serializedInterResultValue
            );

            return interResultValue;
        }
    }
}
