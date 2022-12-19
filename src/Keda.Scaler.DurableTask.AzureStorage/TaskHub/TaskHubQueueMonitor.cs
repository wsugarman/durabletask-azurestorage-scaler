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

internal class TaskHubQueueMonitor : ITaskHubQueueMonitor
{
    private readonly AzureStorageTaskHubInfo _taskHubInfo;
    private readonly QueueServiceClient _queueServiceClient;
    private readonly ILogger _logger;

    public TaskHubQueueMonitor(AzureStorageTaskHubInfo taskHubInfo, QueueServiceClient queueServiceClient, ILogger logger)
    {
        _taskHubInfo = taskHubInfo ?? throw new ArgumentNullException(nameof(taskHubInfo));
        if (taskHubInfo.PartitionCount < 1)
            throw new ArgumentException(SR.InvalidPartitionCountFormat, nameof(taskHubInfo));

        _queueServiceClient = queueServiceClient ?? throw new ArgumentNullException(nameof(queueServiceClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public virtual async ValueTask<TaskHubQueueUsage> GetUsageAsync(CancellationToken cancellationToken = default)
    {
        long workItemQueueMessages;
        long[] controlQueueMessages = new long[_taskHubInfo.PartitionCount];

        // Look at the Control Queues to determine the number of active partitions
        for (int i = 0; i < _taskHubInfo.PartitionCount; i++)
        {
            QueueClient controlQueueClient = _queueServiceClient.GetQueueClient(ControlQueue.GetName(_taskHubInfo.TaskHubName, i));

            try
            {
                QueueProperties properties = await controlQueueClient.GetPropertiesAsync(cancellationToken).ConfigureAwait(false);
                controlQueueMessages[i] = properties.ApproximateMessagesCount;
            }
            catch (RequestFailedException rfe) when (rfe.Status == (int)HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Could not find control queue '{ControlQueueName}'.", controlQueueClient.Name);
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
        catch (RequestFailedException rfe) when (rfe.Status == (int)HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Could not find work item queue '{WorkItemQueueName}'.", workItemQueueClient.Name);
            return TaskHubQueueUsage.None;
        }

        return new TaskHubQueueUsage(controlQueueMessages, workItemQueueMessages);
    }
}
