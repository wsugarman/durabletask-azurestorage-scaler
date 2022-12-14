// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHub;

internal sealed class TaskHubInfo
{
    public string? TaskHubName { get; set; }

    public DateTime CreatedAt { get; set; }

    public int PartitionCount { get; set; }
}
