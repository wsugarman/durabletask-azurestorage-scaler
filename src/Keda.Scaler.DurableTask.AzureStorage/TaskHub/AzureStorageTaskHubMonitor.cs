// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Storage.Queues;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHub;

internal class AzureStorageTaskHubMonitor : ITaskHubMonitor
{
    private readonly TaskHubInfo _taskHubInfo;
    private readonly QueueServiceClient _queueServiceClient;
    private readonly TableServiceClient _tableServiceClient;

    public AzureStorageTaskHubMonitor(TaskHubInfo taskHubInfo, QueueServiceClient queueServiceClient, TableServiceClient tableServiceClient, )
    {
        _taskHubInfo = taskHubInfo ?? throw new ArgumentNullException(nameof(taskHubInfo));
        _queueServiceClient = queueServiceClient ?? throw new ArgumentNullException(nameof(queueServiceClient));
        _tableServiceClient = tableServiceClient ?? throw new ArgumentNullException(nameof(tableServiceClient));
    }

    ValueTask<TaskHubUsage> GetUsageAsync(CancellationToken cancellationToken = default);
}
