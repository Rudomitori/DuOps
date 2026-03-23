using System.Runtime.CompilerServices;
using Dapper;
using DuOps.Core;
using DuOps.Core.InnerResults;
using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;
using DuOps.Core.Storages;
using DuOps.Npgsql.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DuOps.Npgsql;

internal sealed class NpgsqlOperationStorage(
    IConnectionFactory connectionFactory,
    IOptionsMonitor<NpgsqlOperationStorageOptions> optionsMonitor,
    TimeProvider timeProvider,
    ILogger<NpgsqlOperationStorage> logger,
    string storageName
) : IOperationStorage
{
    public async IAsyncEnumerable<IOperationStorageHandle> EnumerateForExecutionAsync(
        OperationQueueId queueId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            var options = optionsMonitor.Get(storageName);
            await using var connection = await connectionFactory.GetConnectionAsync(
                cancellationToken
            );

            var query = NpgsqlOperationStorageQueries.GetNextForExecution(
                queueId,
                options.LockDuration,
                timeProvider.GetUtcNow().UtcDateTime,
                cancellationToken
            );

            var dto = await connection.QuerySingleOrDefaultAsync<GetNextForExecutionDto>(query);

            if (dto is null)
            {
                await Task.Delay(options.GetNextInterval, cancellationToken);
                continue;
            }

            var operationType = new OperationType(dto.Type);
            var operationId = new SerializedOperationId(dto.Id);
            var args = new SerializedOperationArgs(dto.Args);

            var innerResults = await GetAllInnerResultsAsync(
                operationType,
                operationId,
                cancellationToken
            );

            yield return new NpgsqlOperationStorageHandle(
                this,
                timeProvider,
                options,
                operationType,
                operationId,
                args,
                innerResults,
                dto.RetryCount,
                logger
            );
        }
    }

    internal async Task ExtendLockAsync(
        OperationType operationType,
        SerializedOperationId serializedOperationId,
        DateTime newLockedUntil,
        CancellationToken cancellationToken
    )
    {
        await using var connection = await connectionFactory.GetConnectionAsync(cancellationToken);

        var query = NpgsqlOperationStorageQueries.ExtendLock(
            operationType,
            serializedOperationId,
            newLockedUntil,
            cancellationToken
        );

        var affectedRows = await connection.QuerySingleAsync<int>(query);

        if (affectedRows == 0)
            throw new InvalidOperationException(
                $"{operationType}({serializedOperationId}) not found"
            );
    }

    internal async Task RemoveLockAsync(
        OperationType operationType,
        SerializedOperationId serializedOperationId,
        CancellationToken cancellationToken
    )
    {
        await using var connection = await connectionFactory.GetConnectionAsync(cancellationToken);

        var query = NpgsqlOperationStorageQueries.RemoveLock(
            operationType,
            serializedOperationId,
            cancellationToken
        );

        var affectedRows = await connection.ExecuteAsync(query);

        if (affectedRows == 0)
            throw new InvalidOperationException(
                $"{operationType}({serializedOperationId}) not found"
            );
    }

    public async Task<SerializedOperation?> GetByIdOrDefaultAsync(
        OperationType operationType,
        SerializedOperationId serializedOperationId,
        CancellationToken cancellationToken = default
    )
    {
        await using var connection = await connectionFactory.GetConnectionAsync(cancellationToken);

        var dto = await connection.QuerySingleOrDefaultAsync<OperationDto>(
            NpgsqlOperationStorageQueries.GetById(
                operationType,
                serializedOperationId,
                cancellationToken
            )
        );

        return dto is null ? null : MapToSerializedOperation(dto);
    }

    public async Task ScheduleOperationAsync(
        OperationType operationType,
        OperationQueueId queueId,
        SerializedOperationId serializedOperationId,
        SerializedOperationArgs serializedOperationArgs,
        CancellationToken cancellationToken = default
    )
    {
        var now = timeProvider.GetUtcNow();
        var serializedOperation = new SerializedOperation(
            operationType,
            serializedOperationId,
            queueId,
            ScheduledAt: now.DateTime,
            serializedOperationArgs,
            CreatedAt: now.DateTime,
            SerializedOperationState.Active.Instance,
            RetryCount: 0
        );

        await using var connection = await connectionFactory.GetConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(
            NpgsqlOperationStorageQueries.ScheduleOperation(
                MapToDto(serializedOperation),
                cancellationToken
            )
        );
    }

    public async Task<SerializedInnerResult[]> GetAllInnerResultsAsync(
        OperationType operationType,
        SerializedOperationId serializedOperationId,
        CancellationToken cancellationToken = default
    )
    {
        await using var connection = await connectionFactory.GetConnectionAsync(cancellationToken);

        var query = NpgsqlOperationStorageQueries.GetAllInnerResults(
            operationType,
            serializedOperationId,
            cancellationToken
        );

        var dtos = await connection.QueryAsync<InnerResultDto>(query);

        return dtos.Select(dto => new SerializedInnerResult(
                new InnerResultType(dto.InnerResulttype),
                dto.InnerResultId is null ? null : new SerializedInnerResultId(dto.InnerResultId),
                new SerializedInnerResultValue(dto.Value),
                CreatedAt: dto.CreatedAt,
                UpdatedAt: dto.UpdatedAt
            ))
            .ToArray();
    }

    public async Task AddInnerResultsAsync(
        OperationType operationType,
        SerializedOperationId serializedOperationId,
        IReadOnlyList<SerializedInnerResult> innerResults,
        CancellationToken cancellationToken = default
    )
    {
        // TODO: Check operation exists
        await using var connection = await connectionFactory.GetConnectionAsync(cancellationToken);

        var query = NpgsqlOperationStorageQueries.AddInnerResults(
            operationType,
            serializedOperationId,
            innerResults,
            cancellationToken
        );

        await connection.ExecuteAsync(query);
    }

    public async Task ScheduleRetryAsync(
        OperationType operationType,
        SerializedOperationId serializedOperationId,
        DateTime retryAt,
        CancellationToken cancellationToken = default
    )
    {
        await using var connection = await connectionFactory.GetConnectionAsync(cancellationToken);

        var query = NpgsqlOperationStorageQueries.ScheduleRetry(
            operationType,
            serializedOperationId,
            retryAt,
            cancellationToken
        );

        var affectedRows = await connection.ExecuteAsync(query);

        if (affectedRows == 0)
            throw new InvalidOperationException(
                $"{operationType}({serializedOperationId}) not found"
            );
    }

    public async Task RescheduleAsync(
        OperationType operationType,
        SerializedOperationId serializedOperationId,
        DateTime at,
        CancellationToken cancellationToken = default
    )
    {
        await using var connection = await connectionFactory.GetConnectionAsync(cancellationToken);

        var query = NpgsqlOperationStorageQueries.Reschedule(
            operationType,
            serializedOperationId,
            at,
            cancellationToken
        );

        var affectedRows = await connection.ExecuteAsync(query);

        if (affectedRows == 0)
            throw new InvalidOperationException(
                $"{operationType}({serializedOperationId}) not found"
            );
    }

    public async Task CompleteAsync(
        OperationType operationType,
        SerializedOperationId serializedOperationId,
        SerializedOperationResult result,
        DateTime now,
        CancellationToken cancellationToken = default
    )
    {
        await using var connection = await connectionFactory.GetConnectionAsync(cancellationToken);

        var query = NpgsqlOperationStorageQueries.Complete(
            operationType,
            serializedOperationId,
            now,
            result,
            cancellationToken
        );

        var affectedRows = await connection.ExecuteAsync(query);

        if (affectedRows == 0)
            throw new InvalidOperationException(
                $"{operationType}({serializedOperationId}) not found"
            );
    }

    public async Task FailAsync(
        OperationType operationType,
        SerializedOperationId serializedOperationId,
        string reason,
        DateTime now,
        CancellationToken cancellationToken = default
    )
    {
        await using var connection = await connectionFactory.GetConnectionAsync(cancellationToken);

        var query = NpgsqlOperationStorageQueries.Fail(
            operationType,
            serializedOperationId,
            now,
            reason,
            cancellationToken
        );

        var affectedRows = await connection.ExecuteAsync(query);

        if (affectedRows == 0)
            throw new InvalidOperationException(
                $"{operationType}({serializedOperationId}) not found"
            );
    }

    private SerializedOperation MapToSerializedOperation(OperationDto dto)
    {
        return new SerializedOperation(
            new OperationType(dto.Type),
            new SerializedOperationId(dto.Id),
            QueueId: new OperationQueueId(dto.QueueId),
            ScheduledAt: dto.ScheduledAt,
            State: MapToSerializedOperationState(dto),
            Args: new SerializedOperationArgs(dto.Args),
            CreatedAt: dto.CreatedAt,
            RetryCount: dto.RetryCount
        );
    }

    private SerializedOperationState MapToSerializedOperationState(OperationDto dto)
    {
        // TODO: Add custom exception messages
        return (OperationStateDto)dto.State switch
        {
            OperationStateDto.Active => new SerializedOperationState.Active(),
            OperationStateDto.Completed => new SerializedOperationState.Completed(
                At: dto.FinishedAt ?? throw new NullReferenceException(),
                Result: new SerializedOperationResult(
                    dto.Result ?? throw new NullReferenceException()
                )
            ),
            OperationStateDto.Failed => new SerializedOperationState.Failed(
                At: dto.FinishedAt ?? throw new NullReferenceException(),
                Reason: dto.FailReason ?? throw new NullReferenceException()
            ),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    private static OperationDto MapToDto(SerializedOperation operation)
    {
        return new OperationDto(
            Type: operation.Type.Value,
            Id: operation.Id.Value,
            QueueId: operation.QueueId,
            ScheduledAt: operation.ScheduledAt,
            Args: operation.Args.Value,
            State: operation.State switch
            {
                SerializedOperationState.Active => (short)OperationStateDto.Active,
                SerializedOperationState.Failed => (short)OperationStateDto.Failed,
                SerializedOperationState.Completed => (short)OperationStateDto.Completed,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(operation.State),
                    operation.State,
                    "Unknown operation state"
                ),
            },
            CreatedAt: operation.CreatedAt,
            FinishedAt: operation.State switch
            {
                SerializedOperationState.Completed completed => completed.At,
                SerializedOperationState.Failed failed => failed.At,
                _ => null,
            },
            Result: operation.State switch
            {
                SerializedOperationState.Completed finished => finished.Result.Value,
                _ => null,
            },
            RetryCount: operation.RetryCount,
            FailReason: operation.State switch
            {
                SerializedOperationState.Failed failed => failed.Reason,
                _ => null,
            }
        );
    }
}
