// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHub;

internal class AzureStorageTaskHubMonitor : ITaskHubMonitor
{
    private readonly TaskHubInfo _taskHubInfo;
    private readonly QueueServiceClient _queueServiceClient;
    private readonly TableServiceClient _tableServiceClient;
    private readonly ILogger _logger;

    private const string InstancesTableSuffix = "Instances";

    public AzureStorageTaskHubMonitor(
        TaskHubInfo taskHubInfo,
        QueueServiceClient queueServiceClient,
        TableServiceClient tableServiceClient,
        ILogger logger)
    {
        _taskHubInfo = taskHubInfo ?? throw new ArgumentNullException(nameof(taskHubInfo));
        if (taskHubInfo.PartitionCount < 1)
            throw new ArgumentException(SR.InvalidPartitionCountFormat, nameof(taskHubInfo));

        _queueServiceClient = queueServiceClient ?? throw new ArgumentNullException(nameof(queueServiceClient));
        _tableServiceClient = tableServiceClient ?? throw new ArgumentNullException(nameof(tableServiceClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<TaskHubUsage> GetUsageAsync(CancellationToken cancellationToken = default)
    {
        // First check the Task Hub Instances table
        TaskHubUsage? usage = await GetInstancesTableUsage(cancellationToken).ConfigureAwait(false);
        if (usage.GetValueOrDefault() == default)
        {
            // If the table could not be found, or if the table devoid of activity,
            // then look the control and work items queues to ensure we aren't missing anything.
            // For example, a message could have been enqueued before the client could finish writing to the table.
            usage = await GetQueueUsage(cancellationToken).ConfigureAwait(false);
        }

        if (usage is null)
        {
            _logger.LogWarning("Could not find relevant queues or tables. Task Hub must not have completed initialization yet.");
            usage = new TaskHubUsage { CurrentActivityCount = 0, CurrentOrchestrationCount = 0 };
        }

        return usage.GetValueOrDefault();
    }

    private async ValueTask<TaskHubUsage?> GetInstancesTableUsage(CancellationToken cancellationToken)
    {
        TableClient instancesTableClient = _tableServiceClient.GetTableClient(_taskHubInfo.TaskHubName + InstancesTableSuffix);

        try
        {
            AsyncPageable< query = await instancesTableClient.QueryAsync("", cancellationToken: cancellationToken);
        }
    }

    private async ValueTask<TaskHubUsage?> GetQueueUsage(CancellationToken cancellationToken)
    {

    }
}
