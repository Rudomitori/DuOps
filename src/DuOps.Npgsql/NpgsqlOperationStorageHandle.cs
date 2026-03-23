using DuOps.Core.InnerResults;
using DuOps.Core.OperationDefinitions;
using DuOps.Core.Operations;
using DuOps.Core.Storages;
using Microsoft.Extensions.Logging;

namespace DuOps.Npgsql;

internal sealed class NpgsqlOperationStorageHandle : IOperationStorageHandle
{
    private readonly NpgsqlOperationStorage _storage;
    private readonly TimeProvider _timeProvider;
    private readonly NpgsqlOperationStorageOptions _storageOptions;
    private readonly ILogger<NpgsqlOperationStorage> _logger;

    internal NpgsqlOperationStorageHandle(
        NpgsqlOperationStorage storage,
        TimeProvider timeProvider,
        NpgsqlOperationStorageOptions storageOptions,
        OperationType operationType,
        SerializedOperationId serializedOperationId,
        SerializedOperationArgs args,
        SerializedInnerResult[] innerResults,
        int retryCount,
        ILogger<NpgsqlOperationStorage> logger
    )
    {
        _storage = storage;
        _timeProvider = timeProvider;
        _storageOptions = storageOptions;
        OperationType = operationType;
        SerializedOperationId = serializedOperationId;
        OperationArgs = args;
        _innerResults = innerResults.GroupBy(x => x.Type).ToDictionary(x => x.Key, x => x.ToList());

        RetryCount = retryCount;
        _logger = logger;

        StartLockExtending();
    }

    public OperationType OperationType { get; }

    public SerializedOperationId SerializedOperationId { get; }

    public SerializedOperationArgs OperationArgs { get; }

    public int RetryCount { get; }

    public async Task CompleteAsync(
        SerializedOperationResult result,
        CancellationToken cancellationToken
    )
    {
        CheckLock();

        await FlushInnerResultsAsync();
        await _storage.CompleteAsync(
            OperationType,
            SerializedOperationId,
            result,
            _timeProvider.GetUtcNow().DateTime,
            cancellationToken
        );
    }

    public async Task FailAsync(string reason, CancellationToken cancellationToken)
    {
        CheckLock();

        await FlushInnerResultsAsync();
        await _storage.FailAsync(
            OperationType,
            SerializedOperationId,
            reason,
            _timeProvider.GetUtcNow().DateTime,
            cancellationToken
        );
    }

    public async Task RescheduleAsync(DateTime at, CancellationToken cancellationToken)
    {
        CheckLock();

        await _storage.RescheduleAsync(OperationType, SerializedOperationId, at, cancellationToken);
    }

    public async Task ScheduleRetryAsync(DateTime retryAt, CancellationToken cancellationToken)
    {
        CheckLock();

        await _storage.ScheduleRetryAsync(
            OperationType,
            SerializedOperationId,
            retryAt,
            cancellationToken
        );
    }

    #region InnerResults

    private readonly List<SerializedInnerResult> _notFlushedInnerResults = [];
    private readonly Dictionary<InnerResultType, List<SerializedInnerResult>> _innerResults;

    public SerializedInnerResult? GetInnerResultOrDefault(
        InnerResultType type,
        SerializedInnerResultId? id
    )
    {
        return _innerResults.GetValueOrDefault(type)?.FirstOrDefault(x => x.Id == id);
    }

    public SerializedInnerResult[] GetInnerResults(InnerResultType type)
    {
        return _innerResults.GetValueOrDefault(type)?.ToArray() ?? [];
    }

    public void AddInnerResultLazy(SerializedInnerResult innerResult)
    {
        if (!_innerResults.TryGetValue(innerResult.Type, out var list))
        {
            list = [];
        }

        var existedInnerResult = list.FirstOrDefault(x => x.Id == innerResult.Id);

        if (existedInnerResult is not null)
            throw new InvalidOperationException(
                $"inner result {innerResult.Type}({innerResult.Id}) already exists"
            );

        list.Add(innerResult);
        _notFlushedInnerResults.Add(innerResult);
    }

    public async Task FlushInnerResultsAsync()
    {
        if (_notFlushedInnerResults.Count == 0)
            return;

        CheckLock();

        await _storage.AddInnerResultsAsync(
            OperationType,
            SerializedOperationId,
            _notFlushedInnerResults,
            CancellationToken.None
        );

        _notFlushedInnerResults.Clear();
    }

    #endregion

    #region Lock

    private volatile bool _lockAcquired = true;
    private readonly CancellationTokenSource _lockExtendingCancellationTokenSource = new();

    private void StartLockExtending()
    {
        Task.Run(async () =>
        {
            try
            {
                while (true)
                {
                    if (_lockExtendingCancellationTokenSource.IsCancellationRequested)
                        return;

                    await Task.Delay(
                        _storageOptions.LockExtendingInterval,
                        _lockExtendingCancellationTokenSource.Token
                    );

                    var newLockedUntil = _timeProvider.GetUtcNow() + _storageOptions.LockDuration;

                    await _storage.ExtendLockAsync(
                        OperationType,
                        SerializedOperationId,
                        newLockedUntil.DateTime,
                        _lockExtendingCancellationTokenSource.Token
                    );
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                _lockAcquired = false;
                _logger.LogError(
                    e,
                    "Failed to extend lock for {Operationtype}({OperationId})",
                    OperationType,
                    SerializedOperationId
                );
            }
        });
    }

    private void CheckLock()
    {
        if (!_lockAcquired)
            throw new InvalidOperationException("Lock not acquired");
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        await _lockExtendingCancellationTokenSource.CancelAsync();
        _lockExtendingCancellationTokenSource.Dispose();

        await FlushInnerResultsAsync();
        await _storage.RemoveLockAsync(
            OperationType,
            SerializedOperationId,
            CancellationToken.None
        );
    }
}
