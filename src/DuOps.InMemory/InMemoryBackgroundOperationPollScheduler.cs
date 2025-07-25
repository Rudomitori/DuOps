using System.Collections.Concurrent;
using DuOps.Core.OperationDefinitions;
using DuOps.Core.OperationPollers;
using DuOps.Core.Operations;
using DuOps.Core.PollingSchedule;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DuOps.InMemory;

internal sealed class InMemoryBackgroundOperationPollScheduler(IServiceProvider serviceProvider)
    : BackgroundService,
        IOperationPollingScheduler
{
    private readonly ConcurrentQueue<OperationQueueItem> _operationsQueue = new();

    private readonly record struct OperationQueueItem(
        OperationPollingScheduleId PollingScheduleId,
        OperationDiscriminator OperationDiscriminator,
        OperationId operationId
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

            // TODO: Compare polling schedule ids

            await using var scope = serviceProvider.CreateAsyncScope();
            var operationPoller = scope.ServiceProvider.GetRequiredService<IOperationPoller>();

            try
            {
                var operationState = await operationPoller.PollOperation(
                    queueItem.OperationDiscriminator,
                    queueItem.operationId,
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
            catch (Exception)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50), stoppingToken);
                _operationsQueue.Enqueue(queueItem);
            }
        }
    }
}
