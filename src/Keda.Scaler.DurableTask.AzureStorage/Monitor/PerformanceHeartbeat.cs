// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Keda.Scaler.DurableTask.AzureStorage.Monitor;

internal class PerformanceHeartbeat
{
    public int PartitionCount { get; set; }
    public QueueMetric WorkItemQueueMetric { get; set; } = new QueueMetric();
    public IReadOnlyList<QueueMetric> ControlQueueMetrixs { get; set; } = new List<QueueMetric>();

}
