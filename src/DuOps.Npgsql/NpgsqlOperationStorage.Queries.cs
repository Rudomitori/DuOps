using Dapper;
using DuOps.Core.InnerResults;
using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;
using DuOps.Npgsql.Dtos;

namespace DuOps.Npgsql;

internal static class NpgsqlOperationStorageQueries
{
    internal static CommandDefinition GetById(
        OperationType type,
        SerializedOperationId serializedOperationId,
        CancellationToken cancellationToken
    )
    {
        return new CommandDefinition(
            // lang=sql
            """
            -- OperationStorage.GetById
            SELECT
                type as "type",
                id as "Id",
                queue as "Queue",
                scheduled_at as "ScheduledAt",
                args as "Args",
                created_at as "CreatedAt",
                finished_at as "FinishedAt",
                state as "State",
                result as "Result",
                fail_reason as "FailReason",
                retry_count as "RetryCount"
            FROM duops_operations
            WHERE
                type = @type
                AND id = @id
            """,
            new { type = type.Value, id = serializedOperationId.Value },
            cancellationToken: cancellationToken
        );
    }

    internal static CommandDefinition ScheduleOperation(
        OperationDto dto,
        CancellationToken cancellationToken
    )
    {
        return new CommandDefinition(
            // lang=sql
            """
            -- OperationStorage.ScheduleOperation
            INSERT INTO duops_operations (
                type,
                id,
                queue,
                scheduled_at,
                args,
                created_at,
                state,
                retry_count
            )
            VALUES (
                @Type, 
                @Id,
                @Queue,
                @ScheduledAt,
                @Args,
                @CreatedAt,
                10, -- Active
                0
            )
            ON CONFLICT DO NOTHING
            """,
            dto,
            cancellationToken: cancellationToken
        );
    }

    internal static CommandDefinition AddInnerResults(
        OperationType operationType,
        SerializedOperationId serializedOperationId,
        IReadOnlyList<SerializedInnerResult> innerResults,
        CancellationToken cancellationToken
    )
    {
        return new CommandDefinition(
            // lang=sql
            """
            -- OperationStorage.AddInnerResults
            INSERT INTO duops_inner_results (
               operation_type,
               operation_id,
               inner_result_type,
               inner_result_id,
               value,
               created_at
            )
            SELECT
                @OperationType,
                @OperationId,
                unnest(@InnerResultTypes::text[]),
                unnest(@InnerResultIds::text[]),
                unnest(@Values::text[]),
                unnest(@CreatedAts::timestamptz[])
            """,
            new
            {
                OperationType = operationType.Value,
                OperationId = serializedOperationId.Value,
                InnerResultTypes = innerResults.Select(x => x.Type.Value).ToArray(),
                InnerResultIds = innerResults.Select(x => x.Id?.Value).ToArray(),
                Values = innerResults.Select(x => x.Value.Value).ToArray(),
                CreatedAts = innerResults.Select(x => x.CreatedAt).ToArray(),
            },
            cancellationToken: cancellationToken
        );
    }

    internal static CommandDefinition GetAllInnerResults(
        OperationType operationType,
        SerializedOperationId serializedOperationId,
        CancellationToken cancellationToken
    )
    {
        return new CommandDefinition(
            // lang=sql
            """
            -- OperationStorage.GetAllInnerResults
            SELECT 
                inner_result_type as "InnerResulttype",
                inner_result_id as "InnerResultId",
                value as "Value",
                created_at as "CreatedAt",
                updated_at as "UpdatedAt"
            FROM duops_inner_results
            WHERE
                operation_type = @Operationtype
                AND operation_id = @OperationId
            """,
            new { Operationtype = operationType.Value, OperationId = serializedOperationId.Value },
            cancellationToken: cancellationToken
        );
    }

    internal static CommandDefinition Reschedule(
        OperationType operationType,
        SerializedOperationId serializedOperationId,
        DateTime at,
        CancellationToken cancellationToken
    )
    {
        return new CommandDefinition(
            // lang=sql
            """
            -- OperationStorage.Reschedule
            UPDATE duops_operations 
            SET 
                scheduled_at = @At
            WHERE 
                type = @type
                AND id = @Id
            """,
            new
            {
                type = operationType.Value,
                Id = serializedOperationId.Value,
                At = at,
            },
            cancellationToken: cancellationToken
        );
    }

    internal static CommandDefinition ScheduleRetry(
        OperationType operationType,
        SerializedOperationId serializedOperationId,
        DateTime at,
        CancellationToken cancellationToken
    )
    {
        return new CommandDefinition(
            // lang=sql
            """
            -- OperationStorage.ScheduleRetry
            UPDATE duops_operations
            SET 
                scheduled_at = @At,
                retry_count = retry_count + 1
            WHERE
                type = @type
                AND id = @Id
            """,
            new
            {
                type = operationType.Value,
                Id = serializedOperationId.Value,
                At = at,
            },
            cancellationToken: cancellationToken
        );
    }

    internal static CommandDefinition Complete(
        OperationType operationType,
        SerializedOperationId serializedOperationId,
        DateTime now,
        SerializedOperationResult result,
        CancellationToken cancellationToken
    )
    {
        return new CommandDefinition(
            // lang=sql
            """
            --- OperationStorage.Complete
            UPDATE duops_operations
            SET
                scheduled_at = null,
                finished_at = @Now,
                state = 20, -- Completed
                result = @Result
            WHERE
                type = @type
                AND id = @Id
            """,
            new
            {
                type = operationType.Value,
                Id = serializedOperationId.Value,
                Now = now,
                Result = result.Value,
            },
            cancellationToken: cancellationToken
        );
    }

    internal static CommandDefinition Fail(
        OperationType operationType,
        SerializedOperationId serializedOperationId,
        DateTime now,
        string failReason,
        CancellationToken cancellationToken
    )
    {
        return new CommandDefinition(
            // lang=sql
            """
            --- OperationStorage.Fail
            UPDATE duops_operations
            SET
                scheduled_at = null,
                finished_at = @Now,
                state = 30, -- Failed
                fail_reason = @FailReason
            WHERE
                type = @type
                AND id = @Id
            """,
            new
            {
                type = operationType.Value,
                Id = serializedOperationId.Value,
                Now = now,
                FailReason = failReason,
            },
            cancellationToken: cancellationToken
        );
    }

    internal static CommandDefinition GetNextForExecution(
        string queue,
        TimeSpan lockDuration,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        return new CommandDefinition(
            // lang=sql
            """
            -- OperationStorage.GetNextForExecution
            UPDATE duops_operations
            SET
                locked_until = @LockedUntil
            WHERE
                (type, id) = (
                    SELECT type, id
                    FROM duops_operations
                    WHERE
                        queue = @Queue
                        AND state = 10 -- Active
                        AND coalesce(locked_until, scheduled_at) < @Now
                    FOR UPDATE SKIP LOCKED
                    LIMIT 1
                )
            RETURNING
                type as "Type",
                id as "Id",
                args as "Args",
                retry_count as "RetryCount"
            """,
            new
            {
                Queue = queue,
                LockedUntil = now + lockDuration,
                Now = now,
            },
            cancellationToken: cancellationToken
        );
    }

    internal static CommandDefinition RemoveLock(
        OperationType type,
        SerializedOperationId serializedOperationId,
        CancellationToken cancellationToken
    )
    {
        return new CommandDefinition(
            // lang=sql
            """
            -- OperationStorage.RemoveLock
            UPDATE duops_operations
            SET locked_until = null
            WHERE
                type = @Type
                AND id = @Id
            """,
            new { Type = type.Value, Id = serializedOperationId.Value },
            cancellationToken: cancellationToken
        );
    }

    internal static CommandDefinition ExtendLock(
        OperationType type,
        SerializedOperationId serializedOperationId,
        DateTime newLockedUntil,
        CancellationToken cancellationToken
    )
    {
        return new CommandDefinition(
            // lang=sql
            """
            -- OperationStorage.ExtendLock
            UPDATE duops_operations
            SET locked_until = CASE 
                    WHEN locked_until IS NULL THEN NULL 
                    ELSE @NewLockedUntil
                END
            WHERE
                type = @Type
                AND id = @Id
            """,
            new
            {
                Type = type.Value,
                Id = serializedOperationId.Value,
                NewLockedUntil = newLockedUntil,
            },
            cancellationToken: cancellationToken
        );
    }
}
