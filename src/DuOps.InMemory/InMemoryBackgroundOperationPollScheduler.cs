using System.Collections.Concurrent;
using DuOps.Core.OperationDefinitions;
using DuOps.Core.OperationPollers;
using DuOps.Core.Operations;
using DuOps.Core.PollingSchedule;
using DuOps.Core.Storages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DuOps.InMemory;

internal sealed class InMemoryBackgroundOperationPollScheduler(
    IServiceProvider serviceProvider,
    ILogger<InMemoryBackgroundOperationPollScheduler> logger
) : BackgroundService, IOperationPollingScheduler
{
    private readonly ConcurrentQueue<OperationQueueItem> _operationsQueue = new();

    private readonly record struct OperationQueueItem(
        OperationPollingScheduleId PollingScheduleId,
        OperationDiscriminator OperationDiscriminator,
        OperationId OperationId
    );

    public Task<OperationPollingScheduleId> SchedulePolling<TArgs, TResult>(
        IOperationDefinition<TArgs, TResult> operationDefinition,
        OperationId operationId,
        CancellationToken cancellationToken
    )
    {
        var pollingScheduleId = new OperationPollingScheduleId(Guid.NewGuid().ToString());
        var operationQueueItem = new OperationQueueItem(
            pollingScheduleId,
            operationDefinition.Discriminator,
            operationId
        );

        _operationsQueue.Enqueue(operationQueueItem);

        return Task.FromResult(pollingScheduleId);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (!_operationsQueue.TryDequeue(out var queueItem))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50), stoppingToken);
                continue;
            }

            await using var scope = serviceProvider.CreateAsyncScope();
            var operationPoller = scope.ServiceProvider.GetRequiredService<IOperationPoller>();
            var operationStorage = scope.ServiceProvider.GetRequiredService<IOperationStorage>();

            var operation = await GetOperation(
                operationStorage,
                queueItem.OperationDiscriminator,
                queueItem.OperationId,
                stoppingToken
            );

            if (queueItem.PollingScheduleId != operation.PollingScheduleId)
            {
                logger.LogError(
                    "Skip {OperationDiscriminator}({OperationId}), because Operation.PollingScheduleId != enqueued PollingScheduleId",
                    operation.Discriminator,
                    queueItem.OperationId
                );
                continue;
            }

            try
            {
                var operationState = await operationPoller.PollOperation(
                    queueItem.OperationDiscriminator,
                    queueItem.OperationId,
                    stoppingToken
                );

                if (operationState is SerializedOperationState.Yielded)
                {
                    _operationsQueue.Enqueue(queueItem);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception e)
            {
                var reenqueueDelay = TimeSpan.FromMilliseconds(50);

                logger.LogError(
                    e,
                    "Polling of {OperationDiscriminator}({OperationId}) failed. Operation will be reenqueued in {ReenqueueDelay}",
                    queueItem.OperationDiscriminator,
                    queueItem.OperationId,
                    reenqueueDelay
                );
                await Task.Delay(reenqueueDelay, stoppingToken);
                _operationsQueue.Enqueue(queueItem);
            }
        }
    }

    private async Task<SerializedOperation> GetOperation(
        IOperationStorage operationStorage,
        OperationDiscriminator discriminator,
        OperationId operationId,
        CancellationToken cancellationToken
    )
    {
        var attemptsInterval = TimeSpan.FromMilliseconds(10);

        var operation =
            await operationStorage.AwaitOperationHasPollingScheduleIdAndGetByIdOrDefault(
                discriminator,
                operationId,
                attemptsInterval,
                attemptsInterval * 10,
                cancellationToken
            );

        return operation
            ?? throw new InvalidOperationException(
                $"{discriminator.Value}({operationId}) was not found"
            );
    }
}
