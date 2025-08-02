using System.Diagnostics;
using Dapper;
using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;
using DuOps.Core.Operations.InterResults;
using DuOps.Npgsql.Dtos;

namespace DuOps.Npgsql;

internal static class NpgsqlOperationStorageQueries
{
    private const string DtoFields = """
            discriminator as "Discriminator",
            id as "Id",
            polling_schedule_id as "PollingScheduleId",
            started_at as "StartedAt",
            args as "Args",
            state as "State",
            result as "Result",
            waiting_until as "WaitingUntil",
            retrying_at as "RetryingAt",
            retry_count as "RetryCount",
            fail_reason as "FailReason",
            inter_results as "InterResults"
        """;

    internal static CommandDefinition GetById(
        OperationDiscriminator discriminator,
        OperationId operationId,
        CancellationToken cancellationToken
    )
    {
        return new CommandDefinition(
            $"""
            SELECT
            {DtoFields}
            FROM duops_operations
            WHERE
                discriminator = @discriminator
                AND id = @id
            """,
            new { discriminator = discriminator.Value, id = operationId.Value },
            cancellationToken: cancellationToken
        );
    }

    internal static CommandDefinition GetOrAdd(
        OperationDto dto,
        CancellationToken cancellationToken
    )
    {
        return new CommandDefinition(
            """
            INSERT INTO duops_operations (
                discriminator, 
                id,
                polling_schedule_id,
                started_at,
                args,
                state,
                result,
                waiting_until,
                retrying_at,
                retry_count,
                fail_reason,
                inter_results
            )
            VALUES (
                @Discriminator, 
                @Id,
                @PollingScheduleId,
                @StartedAt,
                @Args::jsonb,
                @State,
                @Result::jsonb,
                @WaitingUntil,
                @RetryingAt,
                @RetryCount,
                @FailReason,
                @InterResults::jsonb
            )
            ON CONFLICT DO NOTHING
            """,
            dto,
            cancellationToken: cancellationToken
        );
    }

    internal static CommandDefinition AddNotKeyedInterResult(
        OperationDiscriminator operationDiscriminator,
        OperationId operationId,
        SerializedInterResult interResult,
        CancellationToken cancellationToken
    )
    {
        return new CommandDefinition(
            $"""
            UPDATE duops_operations
            SET inter_results = CASE
                    WHEN state in (40, 50) -- Finished or Failed
                        THEN inter_results
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
                result_discriminator = interResult.Discriminator.Value,
                value = interResult.Value.Value,
            },
            cancellationToken: cancellationToken
        );
    }

    internal static CommandDefinition AddKeyedInterResult(
        OperationDiscriminator operationDiscriminator,
        OperationId operationId,
        SerializedInterResult interResult,
        CancellationToken cancellationToken
    )
    {
        Debug.Assert(interResult.Key is not null);

        return new CommandDefinition(
            $"""
            UPDATE duops_operations
            SET inter_results = CASE
                    WHEN state in (40, 50) -- Finished or Failed
                        THEN inter_results  
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
                result_discriminator = interResult.Discriminator.Value,
                key = interResult.Key.Value.Value,
                value = interResult.Value.Value,
            },
            cancellationToken: cancellationToken
        );
    }

    internal static CommandDefinition SetState(
        OperationDiscriminator operationDiscriminator,
        OperationId operationId,
        SerializedOperationState state,
        CancellationToken cancellationToken
    )
    {
        return state switch
        {
            SerializedOperationState.Created => throw new InvalidOperationException(
                $"Operation.State can not be set to {nameof(SerializedOperationState.Created)}"
            ),
            SerializedOperationState.Waiting waiting => new CommandDefinition(
                $"""
                UPDATE duops_operations
                SET 
                    -- state is Finished or Failed
                    state = CASE WHEN state in (40, 50) THEN state ELSE @state END,
                    waiting_until = CASE WHEN state in (40, 50) THEN waiting_until ELSE @waiting_until END,
                    retry_count = CASE WHEN state in (40, 50) THEN retry_count ELSE null END,
                    retrying_at = CASE WHEN state in (40, 50) THEN retrying_at ELSE null END
                WHERE discriminator = @discriminator AND id = @id
                RETURNING
                {DtoFields}
                """,
                new
                {
                    discriminator = operationDiscriminator.Value,
                    id = operationId.Value,
                    state = OperationStateDto.Waiting,
                    waiting_until = waiting.Until,
                },
                cancellationToken: cancellationToken
            ),
            SerializedOperationState.Retrying retrying => new CommandDefinition(
                $"""
                UPDATE duops_operations
                SET 
                    -- state is Finished or Failed
                    state = CASE WHEN state in (40, 50) THEN state ELSE @state END,
                    waiting_until = CASE WHEN state in (40, 50) THEN waiting_until ELSE null END,
                    retry_count = CASE WHEN state in (40, 50) THEN retry_count ELSE @retry_count END,
                    retrying_at = CASE WHEN state in (40, 50) THEN retrying_at ELSE @retrying_at END
                WHERE discriminator = @discriminator AND id = @id
                RETURNING
                {DtoFields}
                """,
                new
                {
                    discriminator = operationDiscriminator.Value,
                    id = operationId.Value,
                    state = OperationStateDto.Retrying,
                    retry_count = retrying.RetryCount,
                    retrying_at = retrying.At,
                },
                cancellationToken: cancellationToken
            ),
            SerializedOperationState.Finished finished => new CommandDefinition(
                $"""
                UPDATE duops_operations
                SET 
                    -- state is Finished or Failed
                    state = CASE WHEN state in (40, 50) THEN state ELSE @state END,
                    waiting_until = CASE WHEN state in (40, 50) THEN waiting_until ELSE null END,
                    retry_count = CASE WHEN state in (40, 50) THEN retry_count ELSE null END,
                    retrying_at = CASE WHEN state in (40, 50) THEN retrying_at ELSE null END,
                    result = CASE WHEN state in (40, 50) THEN result ELSE @result END
                WHERE discriminator = @discriminator AND id = @id
                RETURNING
                {DtoFields}
                """,
                new
                {
                    discriminator = operationDiscriminator.Value,
                    id = operationId.Value,
                    state = OperationStateDto.Finished,
                    result = finished.Result.Value,
                },
                cancellationToken: cancellationToken
            ),
            SerializedOperationState.Failed failed => new CommandDefinition(
                $"""
                UPDATE duops_operations
                SET 
                    -- state is Finished or Failed
                    state = CASE WHEN state in (40, 50) THEN state ELSE @state END,
                    waiting_until = CASE WHEN state in (40, 50) THEN waiting_until ELSE null END,
                    retry_count = CASE WHEN state in (40, 50) THEN retry_count ELSE null END,
                    retrying_at = CASE WHEN state in (40, 50) THEN retrying_at ELSE null END,
                    fail_reason = CASE WHEN state in (40, 50) THEN fail_reason ELSE @fail_reason END
                WHERE discriminator = @discriminator AND id = @id
                RETURNING
                {DtoFields}
                """,
                new
                {
                    discriminator = operationDiscriminator.Value,
                    id = operationId.Value,
                    state = OperationStateDto.Failed,
                    fail_reason = failed.Reason,
                },
                cancellationToken: cancellationToken
            ),
            _ => throw new ArgumentOutOfRangeException(
                nameof(state),
                state,
                $"Unknown operation state {state}"
            ),
        };
    }

    internal static CommandDefinition GetOrSetPollingScheduleId(
        OperationDiscriminator discriminator,
        OperationId operationId,
        OperationPollingScheduleId scheduleId,
        CancellationToken cancellationToken
    )
    {
        return new CommandDefinition(
            $"""
            UPDATE duops_operations
            SET polling_schedule_id = CASE
                    -- Finished or Failed
                    WHEN state in (40, 50) THEN polling_schedule_id
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
        );
    }

    public static CommandDefinition Delete(
        OperationDiscriminator discriminator,
        OperationId operationId,
        CancellationToken cancellationToken = default
    )
    {
        return new CommandDefinition(
            """
            DELETE FROM duops_operations
            WHERE discriminator = @discriminator AND id = @id
            """,
            new { discriminator = discriminator.Value, id = operationId.Value },
            cancellationToken: cancellationToken
        );
    }
}
