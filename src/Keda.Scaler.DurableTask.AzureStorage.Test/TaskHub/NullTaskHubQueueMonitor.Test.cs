// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Keda.Scaler.DurableTask.AzureStorage.TaskHub;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHub;

public class NullTaskHubQueueMonitorTest
{
    [Fact]
    public async Task Given_GetUsageAsync()
    {
        NullTaskHubQueueMonitor monitor = NullTaskHubQueueMonitor.Instance;
        Assert.Same(TaskHubQueueUsage.None, await monitor.GetUsageAsync(default));
    }
}
