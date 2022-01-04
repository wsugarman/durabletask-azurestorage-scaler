// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Linq;
using DurableTask.AzureStorage.Monitoring;

namespace Keda.Scaler.DurableTask.AzureStorage.Extensions;

internal static class PerformanceHeartbeatExtensions
{
    public static bool IsIdle(this PerformanceHeartbeat heartbeat)
    {
        if (heartbeat is null)
            throw new ArgumentNullException(nameof(heartbeat));

        return heartbeat.WorkItemQueueLatency == TimeSpan.Zero
            && (heartbeat.ControlQueueLatencies is null || heartbeat.ControlQueueLatencies.All(x => x == TimeSpan.Zero));
    }
}
