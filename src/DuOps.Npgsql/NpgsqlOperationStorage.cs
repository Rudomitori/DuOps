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
            new CommandDefinition(
                $"""
                SELECT
                {DtoFields}
                FROM duops_operations
                where
                    discriminator = @discriminator
                    and id = @id
                """,
                new { discriminator = discriminator.Value, id = operationId.Value },
                cancellationToken: cancellationToken
            )
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
            new CommandDefinition(
                $"""
                INSERT INTO duops_operations (
                    discriminator, 
                    id,
                    polling_schedule_id,
                    started_at,
                    args,
                    is_finished,
                    result,
                    inter_results
                )
                VALUES (
                    @Discriminator, 
                    @Id,
                    @PollingScheduleId,
                    @StartedAt,
                    @Args::jsonb,
                    @IsFinished,
                    @Result::jsonb,
                    @InterResults::jsonb
                )
                ON CONFLICT DO NOTHING
                """,
                MapToDto(serializedOperation),
                cancellationToken: cancellationToken
            )
        );

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
            new CommandDefinition(
                $"""
                UPDATE duops_operations
                SET inter_results = CASE
                        WHEN inter_results[@result_discriminator] IS NULL 
                            THEN jsonb_set(
                                inter_results,
                                array[@result_discriminator],
                                to_jsonb(@value)
                            )
                        ELSE inter_results
                    END 
                WHERE discriminator = @discriminator AND id = @id
                RETURNING {DtoFields}
                """,
                new
                {
                    discriminator = operationDiscriminator.Value,
                    id = operationId.Value,
                    result_discriminator = result.Discriminator.Value,
                    value = result.Value.Value,
                },
                cancellationToken: cancellationToken
            )
        );

        #region Post condition checks

        CheckOperationIsFound(operationDiscriminator, operationId, dto);
        CheckOperationIsNotFinishedYet(operationDiscriminator, operationId, dto);

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
        SerializedInterResult result,
        CancellationToken cancellationToken
    )
    {
        Debug.Assert(result.Key is not null);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var dto = await connection.QuerySingleOrDefaultAsync<OperationDto>(
            new CommandDefinition(
                $"""
                UPDATE duops_operations
                SET inter_results = CASE
                        WHEN is_finished THEN inter_results
                        WHEN inter_results[@result_discriminator][@key] IS NOT NULL 
                            THEN inter_results
                        WHEN jsonb_typeof(inter_results[@result_discriminator]) = 'object' 
                            THEN jsonb_set(
                                inter_results,
                                array[@result_discriminator, @key],
                                to_jsonb(@value)
                            )
                        WHEN inter_results[@result_discriminator] IS NULL 
                            THEN jsonb_set(
                                inter_results,
                                array[@result_discriminator],
                                jsonb_build_object(@key, to_jsonb(@value))
                            )
                        ELSE inter_results 
                    END 
                WHERE discriminator = @discriminator AND id = @id
                RETURNING {DtoFields}
                """,
                new
                {
                    discriminator = operationDiscriminator.Value,
                    id = operationId.Value,
                    result_discriminator = result.Discriminator.Value,
                    key = result.Key.Value.Value,
                    value = result.Value.Value,
                },
                cancellationToken: cancellationToken
            )
        );

        #region Post condition checks

        CheckOperationIsFound(operationDiscriminator, operationId, dto);
        CheckOperationIsNotFinishedYet(operationDiscriminator, operationId, dto);

        using var jsonDocument = JsonDocument.Parse(dto.InterResults);

        CheckInterResultsRootElementIsObject(operationDiscriminator, operationId, jsonDocument);

        var property = jsonDocument.RootElement.GetProperty(result.Discriminator.Value);

        if (property.ValueKind is JsonValueKind.String)
            throw new InvalidOperationException(
                $"Can not add result {result.Discriminator} with key {result.Key} for {operationDiscriminator}({operationId})"
                    + $" because non keyed result {result.Discriminator} already exists"
            );

        if (property.ValueKind is not JsonValueKind.Object)
            throw new InvalidOperationException(
                $"{operationDiscriminator}({operationId}).Results[{result.Discriminator}]"
                    + $" has unexpected json value kind {property.ValueKind}"
            );

        var innerProperty = property.GetProperty(result.Key.Value.Value);

        if (innerProperty.ValueKind != JsonValueKind.String)
            throw new InvalidOperationException(
                $"{operationDiscriminator}({operationId}).Results[{result.Discriminator}][{result.Key}]"
                    + $" has unexpected json value kind {property.ValueKind}"
            );

        if (innerProperty.GetString() != result.Value)
            throw new InvalidOperationException(
                $"Can not add result {result.Discriminator} with key {result.Key} for {operationDiscriminator}({operationId})"
                    + $" because result {result.Discriminator} already exists"
            );

        #endregion
    }

    public async Task AddResult(
        OperationDiscriminator discriminator,
        OperationId operationId,
        SerializedOperationResult serializedOperationResult,
        CancellationToken cancellationToken = default
    )
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var dto = await connection.QuerySingleOrDefaultAsync<OperationDto>(
            new CommandDefinition(
                $"""
                UPDATE duops_operations
                SET is_finished = TRUE,
                    result = coalesce(result, @result)
                WHERE discriminator = @discriminator AND id = @id
                RETURNING
                {DtoFields}
                """,
                new
                {
                    discriminator = discriminator.Value,
                    id = operationId.Value,
                    result = serializedOperationResult.Value,
                },
                cancellationToken: cancellationToken
            )
        );

        CheckOperationIsFound(discriminator, operationId, dto);

        if (dto.Result != serializedOperationResult.Value)
            throw new InvalidOperationException(
                $"{discriminator}({operationId}) already has another result"
            );
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
            new CommandDefinition(
                $"""
                UPDATE duops_operations
                SET polling_schedule_id = CASE
                        WHEN is_finished THEN polling_schedule_id
                        ELSE coalesce(polling_schedule_id, @polling_schedule_id)
                    END
                WHERE discriminator = @discriminator AND id = @id
                RETURNING
                {DtoFields}
                """,
                new
                {
                    discriminator = discriminator.Value,
                    id = operationId.Value,
                    polling_schedule_id = scheduleId.Value,
                },
                cancellationToken: cancellationToken
            )
        );

        CheckOperationIsFound(discriminator, operationId, dto);
        CheckOperationIsNotFinishedYet(discriminator, operationId, dto);

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
            new CommandDefinition(
                """
                DELETE FROM duops_operations
                WHERE discriminator = @discriminator AND id = @id
                """,
                new { discriminator = discriminator.Value, id = operationId.Value },
                cancellationToken: cancellationToken
            )
        );
    }

    private SerializedOperation MapToSerializedOperation(OperationDto dto)
    {
        if (!dto.IsFinished)
            Debug.Assert(dto.Result is null);

        return new SerializedOperation(
            new OperationDiscriminator(dto.Discriminator),
            new OperationId(dto.Id),
            dto.PollingScheduleId is null
                ? null
                : new OperationPollingScheduleId(dto.PollingScheduleId),
            dto.StartedAt,
            new SerializedOperationArgs(dto.Args),
            dto.IsFinished
                ? new SerializedOperationState.Finished(new SerializedOperationResult(dto.Result))
                : SerializedOperationState.Yielded.Instance,
            DeserializeInterResults(dto.InterResults)
        );
    }

    private static OperationDto MapToDto(SerializedOperation operation)
    {
        var finishedState = operation.State as SerializedOperationState.Finished;

        return new OperationDto(
            operation.Discriminator.Value,
            operation.Id.Value,
            operation.PollingScheduleId?.Value,
            operation.StartedAt,
            operation.Args.Value,
            IsFinished: finishedState is not null,
            Result: finishedState?.Result.Value,
            SerializedInterResults(operation.InterResults)
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

    private const string DtoFields = """
            discriminator as "Discriminator",
            id as "Id",
            polling_schedule_id as "PollingScheduleId",
            started_at as "StartedAt",
            args as "Args",
            is_finished as "IsFinished",
            result as "Result",
            inter_results as "InterResults"
        """;

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

    private void CheckOperationIsNotFinishedYet(
        OperationDiscriminator discriminator,
        OperationId operationId,
        OperationDto dto
    )
    {
        if (dto.IsFinished)
            throw new InvalidOperationException(
                $"{discriminator}({operationId}) is already finished"
            );
    }

    #endregion
}
