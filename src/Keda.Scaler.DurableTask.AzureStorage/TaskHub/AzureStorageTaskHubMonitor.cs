// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Keda.Scaler.DurableTask.AzureStorage.Queues;
using Microsoft.Extensions.Logging;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHub;

internal class AzureStorageTaskHubMonitor : ITaskHubMonitor
{
    private readonly AzureStorageTaskHubInfo _taskHubInfo;
    private readonly QueueServiceClient _queueServiceClient;
    private readonly ILogger _logger;

    public AzureStorageTaskHubMonitor(AzureStorageTaskHubInfo taskHubInfo, QueueServiceClient queueServiceClient, ILogger logger)
    {
        _taskHubInfo = taskHubInfo ?? throw new ArgumentNullException(nameof(taskHubInfo));
        if (taskHubInfo.PartitionCount < 1)
            throw new ArgumentException(SR.InvalidPartitionCountFormat, nameof(taskHubInfo));

        _queueServiceClient = queueServiceClient ?? throw new ArgumentNullException(nameof(queueServiceClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<TaskHubUsage> GetUsageAsync(CancellationToken cancellationToken = default)
    {
        int activePartitionCount = 0, activeWorkItemCount = 0;

        // Look at the Control Queues to determine the number of active partitions
        for (int i = 0; i < _taskHubInfo.PartitionCount; i++)
        {
            QueueClient controlQueueClient = _queueServiceClient.GetQueueClient(ControlQueue.GetName(_taskHubInfo.TaskHubName, i));

            try
            {
                QueueProperties properties = await controlQueueClient.GetPropertiesAsync(cancellationToken).ConfigureAwait(false);
                if (properties.ApproximateMessagesCount > 0)
                    activePartitionCount++;
            }
            catch (RequestFailedException rfe) when (rfe.Status == (int)HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Could not find control queue '{ControlQueueName}'.", controlQueueClient.Name);
                return default;
            }
        }

        // Look at the Work Item queue to determine the number of active activities, events, etc
        QueueClient workItemQueueClient = _queueServiceClient.GetQueueClient(WorkItemQueue.GetName(_taskHubInfo.TaskHubName));

        try
        {
            QueueProperties properties = await workItemQueueClient.GetPropertiesAsync(cancellationToken).ConfigureAwait(false);
            activeWorkItemCount = properties.ApproximateMessagesCount;
        }
        catch (RequestFailedException rfe) when (rfe.Status == (int)HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Could not find work item queue '{WorkItemQueueName}'.", workItemQueueClient.Name);
            return default;
        }

        return new TaskHubUsage
        {
            ActiveOrchestrationCount = activePartitionCount,
            ActiveWorkItemCount = activeWorkItemCount,
        };
    }
}
