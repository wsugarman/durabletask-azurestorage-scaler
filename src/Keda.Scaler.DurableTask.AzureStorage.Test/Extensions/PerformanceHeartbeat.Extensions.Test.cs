// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using DurableTask.AzureStorage.Monitoring;
using Keda.Scaler.DurableTask.AzureStorage.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Extensions;

[TestClass]
public class PerformanceHeartbeatExtensionsTest
{
    [TestMethod]
    public void IsIdle()
    {
        PerformanceHeartbeat heartbeat;

        Assert.ThrowsException<ArgumentNullException>(() => PerformanceHeartbeatExtensions.IsIdle(null!));

        // Default heartbeat is considered "idle"
        Assert.IsTrue(new PerformanceHeartbeat().IsIdle());
        Assert.IsTrue(new PerformanceHeartbeat { ControlQueueLatencies = new List<TimeSpan> { TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero } }.IsIdle());

        // Work Items being processed
        heartbeat = new PerformanceHeartbeat { WorkItemQueueLatency = TimeSpan.FromSeconds(1) };
        Assert.IsFalse(heartbeat.IsIdle());

        // Control Queue is processing orchestrations, actors, etc
        heartbeat = new PerformanceHeartbeat { ControlQueueLatencies = new List<TimeSpan> { TimeSpan.Zero, TimeSpan.FromSeconds(2) } };
        Assert.IsFalse(heartbeat.IsIdle());
    }
}
