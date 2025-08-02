using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Dapper;
using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;
using DuOps.Core.Operations.InterResults;
using DuOps.Core.Operations.InterResults.Definitions;
using DuOps.Core.Storages;
using DuOps.Npgsql.Dtos;
using Npgsql;

namespace DuOps.Npgsql;

internal sealed class NpgsqlOperationStorage(NpgsqlDataSource dataSource) : IOperationStorage
{
    public async Task<SerializedOperation?> GetByIdOrDefault(
        OperationDiscriminator discriminator,
        OperationId operationId,
        CancellationToken cancellationToken = default
    )
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var dto = await connection.QuerySingleOrDefaultAsync<OperationDto>(
            NpgsqlOperationStorageQueries.GetById(discriminator, operationId, cancellationToken)
        );

        return dto is null ? null : MapToSerializedOperation(dto);
    }

    public async Task<SerializedOperation> GetOrAdd(
        SerializedOperation serializedOperation,
        CancellationToken cancellationToken = default
    )
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var affectedRows = await connection.ExecuteAsync(
            NpgsqlOperationStorageQueries.GetOrAdd(MapToDto(serializedOperation), cancellationToken)
        );

        // TODO: Avoid null on race condition
        return affectedRows == 1
            ? serializedOperation
            : await GetByIdOrDefault(
                serializedOperation.Discriminator,
                serializedOperation.Id,
                CancellationToken.None
            );
    }

    public Task AddInterResult(
        OperationDiscriminator operationDiscriminator,
        OperationId operationId,
        SerializedInterResult result,
        CancellationToken cancellationToken = default
    )
    {
        return result.Key is null
            ? AddNotKeyedInterResult(operationDiscriminator, operationId, result, cancellationToken)
            : AddKeyedInterResult(operationDiscriminator, operationId, result, cancellationToken);
    }

    public async Task SetState(
        OperationDiscriminator operationDiscriminator,
        OperationId operationId,
        SerializedOperationState state,
        CancellationToken cancellationToken = default
    )
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var dto = await connection.QuerySingleOrDefaultAsync<OperationDto>(
            NpgsqlOperationStorageQueries.SetState(
                operationDiscriminator,
                operationId,
                state,
                cancellationToken
            )
        );

        CheckOperationIsFound(operationDiscriminator, operationId, dto);
        var stateFromDb = MapToSerializedOperationState(dto);

        var stateIsSet = (state, stateFromDb) switch
        {
            (SerializedOperationState.Yielded, SerializedOperationState.Yielded) => true,
            (SerializedOperationState.Waiting w1, SerializedOperationState.Waiting w2) =>
                w1.Until.Millisecond == w2.Until.Millisecond,
            (SerializedOperationState.Retrying r1, SerializedOperationState.Retrying r2) =>
                r1.At.Millisecond == r2.At.Millisecond && r1.RetryCount == r2.RetryCount,
            _ => state == stateFromDb,
        };

        if (stateIsSet)
            return;

        // TODO: Separate different cases
        // TODO: Use custom exception
        throw new InvalidOperationException("Failed to set operation state");
    }

    private async Task AddNotKeyedInterResult(
        OperationDiscriminator operationDiscriminator,
        OperationId operationId,
        SerializedInterResult result,
        CancellationToken cancellationToken
    )
    {
        Debug.Assert(result.Key is null);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var dto = await connection.QuerySingleOrDefaultAsync<OperationDto>(
            NpgsqlOperationStorageQueries.AddNotKeyedInterResult(
                operationDiscriminator,
                operationId,
                result,
                cancellationToken
            )
        );

        #region Post condition checks

        CheckOperationIsFound(operationDiscriminator, operationId, dto);
        CheckOperationStateIsNotFinal(operationDiscriminator, operationId, dto);

        using var jsonDocument = JsonDocument.Parse(dto.InterResults);

        CheckInterResultsRootElementIsObject(operationDiscriminator, operationId, jsonDocument);

        var property = jsonDocument.RootElement.GetProperty(result.Discriminator.Value);

        if (property.ValueKind is JsonValueKind.Object)
            throw new InvalidOperationException(
                $"Can not add result {result.Discriminator} for {operationDiscriminator}({operationId})"
                    + $" because keyed result {result.Discriminator} already exists"
            );

        if (property.ValueKind is not JsonValueKind.String)
            throw new InvalidOperationException(
                $"{operationDiscriminator}({operationId}).Results[{result.Discriminator}]"
                    + $" has unexpected json value kind {property.ValueKind}"
            );

        if (property.GetString() != result.Value)
            throw new InvalidOperationException(
                $"Can not add result {result.Discriminator} for {operationDiscriminator}({operationId})"
                    + $" because result {result.Discriminator} already exists"
            );

        #endregion
    }

    private async Task AddKeyedInterResult(
        OperationDiscriminator operationDiscriminator,
        OperationId operationId,
        SerializedInterResult interResult,
        CancellationToken cancellationToken
    )
    {
        Debug.Assert(interResult.Key is not null);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var dto = await connection.QuerySingleOrDefaultAsync<OperationDto>(
            NpgsqlOperationStorageQueries.AddKeyedInterResult(
                operationDiscriminator,
                operationId,
                interResult,
                cancellationToken
            )
        );

        #region Post condition checks

        CheckOperationIsFound(operationDiscriminator, operationId, dto);
        CheckOperationStateIsNotFinal(operationDiscriminator, operationId, dto);

        using var jsonDocument = JsonDocument.Parse(dto.InterResults);

        CheckInterResultsRootElementIsObject(operationDiscriminator, operationId, jsonDocument);

        var property = jsonDocument.RootElement.GetProperty(interResult.Discriminator.Value);

        if (property.ValueKind is JsonValueKind.String)
            throw new InvalidOperationException(
                $"Can not add result {interResult.Discriminator} with key {interResult.Key} for {operationDiscriminator}({operationId})"
                    + $" because non keyed result {interResult.Discriminator} already exists"
            );

        if (property.ValueKind is not JsonValueKind.Object)
            throw new InvalidOperationException(
                $"{operationDiscriminator}({operationId}).Results[{interResult.Discriminator}]"
                    + $" has unexpected json value kind {property.ValueKind}"
            );

        var innerProperty = property.GetProperty(interResult.Key.Value.Value);

        if (innerProperty.ValueKind != JsonValueKind.String)
            throw new InvalidOperationException(
                $"{operationDiscriminator}({operationId}).Results[{interResult.Discriminator}][{interResult.Key}]"
                    + $" has unexpected json value kind {property.ValueKind}"
            );

        if (innerProperty.GetString() != interResult.Value)
            throw new InvalidOperationException(
                $"Can not add result {interResult.Discriminator} with key {interResult.Key} for {operationDiscriminator}({operationId})"
                    + $" because result {interResult.Discriminator} already exists"
            );

        #endregion
    }

    public async Task<OperationPollingScheduleId> GetOrSetPollingScheduleId(
        OperationDiscriminator discriminator,
        OperationId operationId,
        OperationPollingScheduleId scheduleId,
        CancellationToken cancellationToken = default
    )
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var dto = await connection.QuerySingleOrDefaultAsync<OperationDto>(
            NpgsqlOperationStorageQueries.GetOrSetPollingScheduleId(
                discriminator,
                operationId,
                scheduleId,
                cancellationToken
            )
        );

        CheckOperationIsFound(discriminator, operationId, dto);
        CheckOperationStateIsNotFinal(discriminator, operationId, dto);

        return new OperationPollingScheduleId(dto.PollingScheduleId!);
    }

    public async Task Delete(
        OperationDiscriminator discriminator,
        OperationId operationId,
        CancellationToken cancellationToken = default
    )
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(
            NpgsqlOperationStorageQueries.Delete(discriminator, operationId, cancellationToken)
        );
    }

    private SerializedOperation MapToSerializedOperation(OperationDto dto)
    {
        return new SerializedOperation(
            new OperationDiscriminator(dto.Discriminator),
            new OperationId(dto.Id),
            dto.PollingScheduleId is null
                ? null
                : new OperationPollingScheduleId(dto.PollingScheduleId),
            dto.StartedAt,
            new SerializedOperationArgs(dto.Args),
            State: MapToSerializedOperationState(dto),
            DeserializeInterResults(dto.InterResults)
        );
    }

    private SerializedOperationState MapToSerializedOperationState(OperationDto dto)
    {
        // TODO: Add custom exception messages
        return dto.State switch
        {
            OperationStateDto.Created => new SerializedOperationState.Created(),
            OperationStateDto.Waiting => new SerializedOperationState.Waiting(
                dto.WaitingUntil ?? throw new NullReferenceException()
            ),
            OperationStateDto.Retrying => new SerializedOperationState.Retrying(
                dto.RetryingAt ?? throw new NullReferenceException(),
                dto.RetryCount ?? throw new NullReferenceException()
            ),
            OperationStateDto.Finished => new SerializedOperationState.Finished(
                new SerializedOperationResult(dto.Result ?? throw new NullReferenceException())
            ),
            OperationStateDto.Failed => new SerializedOperationState.Failed(
                dto.FailReason ?? throw new NullReferenceException()
            ),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    private static OperationDto MapToDto(SerializedOperation operation)
    {
        return new OperationDto(
            operation.Discriminator.Value,
            operation.Id.Value,
            operation.PollingScheduleId?.Value,
            operation.StartedAt,
            operation.Args.Value,
            State: operation.State switch
            {
                SerializedOperationState.Created => OperationStateDto.Created,
                SerializedOperationState.Failed => OperationStateDto.Failed,
                SerializedOperationState.Finished => OperationStateDto.Finished,
                SerializedOperationState.Retrying => OperationStateDto.Retrying,
                SerializedOperationState.Waiting => OperationStateDto.Waiting,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(operation.State),
                    operation.State,
                    "Unknown operation state"
                ),
            },
            Result: operation.State switch
            {
                SerializedOperationState.Finished finished => finished.Result.Value,
                _ => null,
            },
            WaitingUntil: operation.State switch
            {
                SerializedOperationState.Waiting waiting => waiting.Until.UtcDateTime,
                _ => null,
            },
            RetryingAt: operation.State switch
            {
                SerializedOperationState.Retrying retrying => retrying.At.UtcDateTime,
                _ => null,
            },
            RetryCount: operation.State switch
            {
                SerializedOperationState.Retrying retrying => retrying.RetryCount,
                _ => null,
            },
            FailReason: operation.State switch
            {
                SerializedOperationState.Failed failed => failed.Reason,
                _ => null,
            },
            InterResults: SerializedInterResults(operation.InterResults)
        );
    }

    private static string SerializedInterResults(
        IReadOnlyCollection<SerializedInterResult> interResults
    )
    {
        var dictionary = interResults
            .GroupBy(x => x.Discriminator)
            .ToDictionary(
                x => x.Key.Value,
                object (x) =>
                {
                    var results = x.ToArray();

                    if (results.Any(y => y.Key is not null))
                    {
                        Debug.Assert(results.All(y => y.Key is not null));
                        Debug.Assert(results.DistinctBy(y => y.Key).Count() == results.Length);

                        return results.ToDictionary(y => y.Key!.Value, y => y.Value);
                    }

                    Debug.Assert(results.Length == 1);

                    return results[0].Value;
                }
            );

        return JsonSerializer.Serialize(dictionary);
    }

    private static List<SerializedInterResult> DeserializeInterResults(string json)
    {
        var interResults = new List<SerializedInterResult>();

        using var jsonDocument = JsonDocument.Parse(json);
        foreach (var property in jsonDocument.RootElement.EnumerateObject())
        {
            var discriminator = new InterResultDiscriminator(property.Name);

            switch (property.Value.ValueKind)
            {
                case JsonValueKind.String:
                {
                    var value = property.Value.GetString()!;

                    var interResult = new SerializedInterResult(
                        discriminator,
                        Key: null,
                        new SerializedInterResultValue(value)
                    );
                    interResults.Add(interResult);
                    continue;
                }
                case JsonValueKind.Object:
                {
                    foreach (var innerProperty in property.Value.EnumerateObject())
                    {
                        var key = new SerializedInterResultKey(innerProperty.Name);

                        if (innerProperty.Value.ValueKind != JsonValueKind.String)
                            throw new InvalidOperationException(
                                $"Internal result {discriminator}[{key}] contains unexpected json value kind {innerProperty.Value.ValueKind}"
                            );

                        var interResult = new SerializedInterResult(
                            discriminator,
                            key,
                            new SerializedInterResultValue(innerProperty.Value.GetString()!)
                        );
                        interResults.Add(interResult);
                    }
                    continue;
                }
                default:
                    throw new InvalidOperationException(
                        $"Internal result {discriminator} contains unexpected json value kind {property.Value.ValueKind}"
                    );
            }
        }

        return interResults;
    }

    #region Common checks

    private void CheckInterResultsRootElementIsObject(
        OperationDiscriminator discriminator,
        OperationId operationId,
        JsonDocument jsonDocument
    )
    {
        if (jsonDocument.RootElement.ValueKind is not JsonValueKind.Object)
            throw new InvalidOperationException(
                $"{discriminator}({operationId}) has corrupted inter results"
                    + $" with json value kind {jsonDocument.RootElement.ValueKind}"
            );
    }

    private void CheckOperationIsFound(
        OperationDiscriminator discriminator,
        OperationId operationId,
        [NotNull] OperationDto? dto
    )
    {
        if (dto is null)
            throw new InvalidOperationException($"{discriminator}({operationId}) was not found");
    }

    private void CheckOperationStateIsNotFinal(
        OperationDiscriminator discriminator,
        OperationId operationId,
        OperationDto dto
    )
    {
        if (dto.State is OperationStateDto.Finished or OperationStateDto.Failed)
            throw new InvalidOperationException(
                $"{discriminator}({operationId}).State = {dto.State}"
            );
    }

    #endregion
}
