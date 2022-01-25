// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;

namespace Keda.Scaler.DurableTask.AzureStorage.Monitor;

internal class QueueMetric
{
    public TimeSpan Latency { get; set; }
    public int Length { get; set; }
}
