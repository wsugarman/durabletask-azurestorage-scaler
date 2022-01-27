// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Keda.Scaler.DurableTask.AzureStorage.Monitor;

/// <summary>
///  Represents performance of durable task.
/// </summary>
[ExcludeFromCodeCoverage]
internal class PerformanceHeartbeat
{
    /// <summary>
    /// Gets or sets the number of partitions configured in the task hub.
    /// </summary>
    public int PartitionCount { get; set; }

    /// <summary>
    /// Gets or sets metric of work item queue.
    /// </summary>
    public QueueMetric WorkItemQueueMetric { get; set; } = new QueueMetric();

    /// <summary>
    /// Gets or sets metrics of control queues.
    /// </summary>
    public IReadOnlyList<QueueMetric> ControlQueueMetrics { get; set; } = new List<QueueMetric>();

}
