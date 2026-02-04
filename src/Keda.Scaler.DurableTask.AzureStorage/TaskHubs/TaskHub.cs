// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHubs;

internal partial class TaskHub(ITaskHubPartitionManager partitionManager, QueueServiceClient queueServiceClient, IOptionsSnapshot<TaskHubOptions> options, ILoggerFactory loggerFactory) : ITaskHub
{
    private readonly ITaskHubPartitionManager _partitionManager = partitionManager ?? throw new ArgumentNullException(nameof(partitionManager));
    private readonly QueueServiceClient _queueServiceClient = queueServiceClient ?? throw new ArgumentNullException(nameof(queueServiceClient));
    private readonly TaskHubOptions _options = options?.Get(default) ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger _logger = loggerFactory?.CreateLogger(LogCategories.Default) ?? throw new ArgumentNullException(nameof(loggerFactory));

    public virtual async ValueTask<TaskHubQueueUsage> GetUsageAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> partitionIds = await _partitionManager.GetPartitionsAsync(cancellationToken);
        if (partitionIds.Count is 0)
            return TaskHubQueueUsage.None;

        int workItemQueueMessages;
        int[] controlQueueMessages = new int[partitionIds.Count];

        // Look at the Control Queues to determine the number of active partitions
        for (int i = 0; i < partitionIds.Count; i++)
        {
            QueueClient controlQueueClient = _queueServiceClient.GetQueueClient(partitionIds[i]);

            try
            {
                QueueProperties properties = await controlQueueClient.GetPropertiesAsync(cancellationToken);
                controlQueueMessages[i] = properties.ApproximateMessagesCount;
            }
            catch (RequestFailedException rfe) when (rfe.Status is (int)HttpStatusCode.NotFound)
            {
                LogMissingControlQueue(_logger, controlQueueClient.Name);
                return TaskHubQueueUsage.None;
            }
        }

        // Look at the Work Item queue to determine the number of active activities, events, etc
        QueueClient workItemQueueClient = _queueServiceClient.GetQueueClient(WorkItemQueue.GetName(_options.TaskHubName));

        try
        {
            QueueProperties properties = await workItemQueueClient.GetPropertiesAsync(cancellationToken);
            workItemQueueMessages = properties.ApproximateMessagesCount;
        }
        catch (RequestFailedException rfe) when (rfe.Status is (int)HttpStatusCode.NotFound)
        {
            LogMissingWorkItemQueue(_logger, workItemQueueClient.Name);
            return TaskHubQueueUsage.None;
        }

        LogTaskHubQueues(_logger, workItemQueueMessages, string.Join(", ", controlQueueMessages));
        return new TaskHubQueueUsage(controlQueueMessages, workItemQueueMessages);
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Could not find control queue '{ControlQueueName}'.")]
    private static partial void LogMissingControlQueue(ILogger logger, string controlQueueName);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Could not find work item queue '{WorkItemQueueName}'.")]
    private static partial void LogMissingWorkItemQueue(ILogger logger, string workItemQueueName);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Found {WorkItemCount} work item messages and the following control queue message counts [{ControlCounts}].")]
    private static partial void LogTaskHubQueues(ILogger logger, int workItemCount, string controlCounts);
}
