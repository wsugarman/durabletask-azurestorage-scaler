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

internal class TaskHubQueueMonitor(AzureStorageTaskHubInfo taskHubInfo, QueueServiceClient queueServiceClient, ILogger logger) : ITaskHubQueueMonitor
{
    private readonly AzureStorageTaskHubInfo _taskHubInfo = taskHubInfo ?? throw new ArgumentNullException(nameof(taskHubInfo));
    private readonly QueueServiceClient _queueServiceClient = queueServiceClient ?? throw new ArgumentNullException(nameof(queueServiceClient));
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public virtual async ValueTask<TaskHubQueueUsage> GetUsageAsync(CancellationToken cancellationToken = default)
    {
        int workItemQueueMessages;
        int[] controlQueueMessages = new int[_taskHubInfo.PartitionCount];

        // Look at the Control Queues to determine the number of active partitions
        for (int i = 0; i < _taskHubInfo.PartitionCount; i++)
        {
            QueueClient controlQueueClient = _queueServiceClient.GetQueueClient(ControlQueue.GetName(_taskHubInfo.TaskHubName, i));

            try
            {
                QueueProperties properties = await controlQueueClient.GetPropertiesAsync(cancellationToken).ConfigureAwait(false);
                controlQueueMessages[i] = properties.ApproximateMessagesCount;
            }
            catch (RequestFailedException rfe) when (rfe.Status is (int)HttpStatusCode.NotFound)
            {
                _logger.CouldNotFindControlQueue(controlQueueClient.Name);
                return TaskHubQueueUsage.None;
            }
        }

        // Look at the Work Item queue to determine the number of active activities, events, etc
        QueueClient workItemQueueClient = _queueServiceClient.GetQueueClient(WorkItemQueue.GetName(_taskHubInfo.TaskHubName));

        try
        {
            QueueProperties properties = await workItemQueueClient.GetPropertiesAsync(cancellationToken).ConfigureAwait(false);
            workItemQueueMessages = properties.ApproximateMessagesCount;
        }
        catch (RequestFailedException rfe) when (rfe.Status is (int)HttpStatusCode.NotFound)
        {
            _logger.CouldNotFindWorkItemQueue(workItemQueueClient.Name);
            return TaskHubQueueUsage.None;
        }

        _logger.FoundTaskHubQueues(workItemQueueMessages, string.Join(", ", controlQueueMessages));
        return new TaskHubQueueUsage(controlQueueMessages, workItemQueueMessages);
    }
}
