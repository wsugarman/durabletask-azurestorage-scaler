// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Keda.Scaler.DurableTask.AzureStorage.TaskHub;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHub;

public class NullTaskHubQueueMonitorTest
{
    [TestMethod]
    public async Task GetUsageAsync()
    {
        NullTaskHubQueueMonitor monitor = NullTaskHubQueueMonitor.Instance;
        TaskHubQueueUsage actual = await monitor.GetUsageAsync(default).ConfigureAwait(false);

        Assert.IsTrue(actual.HasActivity);
        Assert.AreEqual(0, actual.ControlQueueMessages.Count);
        Assert.AreEqual(0, actual.WorkItemQueueMessages);
    }
}
