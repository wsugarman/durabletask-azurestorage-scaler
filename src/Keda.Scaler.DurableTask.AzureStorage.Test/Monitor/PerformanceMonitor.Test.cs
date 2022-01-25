// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Keda.Scaler.DurableTask.AzureStorage.Monitor;
using Keda.Scaler.DurableTask.AzureStorage.Test.Mocks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Moq;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Monitor;

[TestClass]
public class PerformanceMonitorTest
{
    [TestMethod]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Durable Task Framework use lowercase for Queue name")]
    public async Task GetHeartbeatAsync()
    {
        string taskHubName = "TestHub";
        string workItemQueueName = $"{taskHubName.ToLowerInvariant()}-workitems";
        var workItemQueue = new Mock<MockCloudQueue>();
        var controlQueue1Name = $"{taskHubName.ToLowerInvariant()}-control-00";
        var controlQueue1 = new Mock<MockCloudQueue>();
        var controlQueue2Name = $"{taskHubName.ToLowerInvariant()}-control-01";
        var controlQueue2 = new Mock<MockCloudQueue>();
        var controlQueue3Name = $"{taskHubName.ToLowerInvariant()}-control-02";
        var controlQueue3 = new Mock<MockCloudQueue>();
        var controlQueue4Name = $"{taskHubName.ToLowerInvariant()}-control-03";
        var controlQueue4 = new Mock<MockCloudQueue>();

        Dictionary<string, CloudQueue> dict = new Dictionary<string, CloudQueue>();
        dict.Add(workItemQueueName, workItemQueue.Object);
        dict.Add(controlQueue1Name, controlQueue1.Object);
        dict.Add(controlQueue2Name, controlQueue2.Object);
        dict.Add(controlQueue3Name, controlQueue3.Object);
        dict.Add(controlQueue4Name, controlQueue4.Object);

        var client = new Mock<MockCloudQueueClient>();
        client.Setup(x => x.GetQueueReference(It.IsAny<string>()))
            .Returns<string>(input => dict[input]);

        PerformanceMonitorSettings settings = new PerformanceMonitorSettings()
        {
            PartitionCount = 4,
            TaskHubName = taskHubName
        };

        PerformanceMonitor monitor = new PerformanceMonitor(client.Object, settings, NullLogger.Instance);
        PerformanceHeartbeat? heartbeat = await monitor.GetHeartbeatAsync().ConfigureAwait(false);
        Assert.IsNotNull(heartbeat);
        Assert.AreEqual(heartbeat.PartitionCount, 4);
        Assert.IsTrue(heartbeat.ControlQueueMetrixs.All(item => item.Latency == TimeSpan.Zero && item.Length == 0));
        Assert.AreEqual(heartbeat.WorkItemQueueMetric.Length, 0);
        Assert.AreEqual(heartbeat.WorkItemQueueMetric.Latency, TimeSpan.Zero);
    }
}
