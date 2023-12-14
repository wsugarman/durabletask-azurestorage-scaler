// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHub;

internal sealed class AzureStorageTaskHubInfo
{
    public DateTimeOffset CreatedAt { get; }

    public int PartitionCount { get; }

    public string TaskHubName { get; }

    public AzureStorageTaskHubInfo(DateTimeOffset createdAt, int partitionCount, string taskHubName)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(partitionCount, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(partitionCount, 15);
        ArgumentException.ThrowIfNullOrWhiteSpace(taskHubName);

        CreatedAt = createdAt;
        PartitionCount = partitionCount;
        TaskHubName = taskHubName;
    }
}
