// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;

namespace Keda.Scaler.DurableTask.AzureStorage.Monitor;

/// <summary>
/// Class contains metric data of cloud queue.
/// </summary>
internal class QueueMetric
{
    /// <summary>
    /// Gets or sets the approximate age of the first work-item queue message.
    /// </summary>
    public TimeSpan Latency { get; set; }

    /// <summary>
    /// Gets or sets the number of messages in the queue.
    /// </summary>
    public int Length { get; set; }
}
