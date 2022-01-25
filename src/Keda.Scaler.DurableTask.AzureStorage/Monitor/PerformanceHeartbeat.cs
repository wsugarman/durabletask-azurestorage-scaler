// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Keda.Scaler.DurableTask.AzureStorage.Monitor;

/// <summary>
///  Represents performance of durable task.
/// </summary>
internal class PerformanceHeartbeat
{
    /// <summary>
    /// Gets or sets the number of partitions configured in the task hub.
    /// </summary>
    public int PartitionCount { get; set; }

    /// <summary>
    /// Gets or sets metric of work item queue.
    /// </summary>
    public CloudQueueMetric WorkItemQueueMetric { get; set; } = new CloudQueueMetric();

    /// <summary>
    /// Gets or sets metrics of control queues.
    /// </summary>
    public IReadOnlyList<CloudQueueMetric> ControlQueueMetrics { get; set; } = new List<CloudQueueMetric>();

}
