// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHub;

internal sealed class NullTaskHubQueueMonitor : ITaskHubQueueMonitor
{
    public static NullTaskHubQueueMonitor Instance { get; } = new NullTaskHubQueueMonitor();

    private NullTaskHubQueueMonitor()
    { }

    public ValueTask<TaskHubQueueUsage> GetUsageAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(TaskHubQueueUsage.None);
}
