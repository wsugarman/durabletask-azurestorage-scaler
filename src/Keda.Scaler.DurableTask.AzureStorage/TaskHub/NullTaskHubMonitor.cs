// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHub;

internal sealed class NullTaskHubMonitor : ITaskHubMonitor
{
    public static ITaskHubMonitor Instance { get; } = new NullTaskHubMonitor();

    private NullTaskHubMonitor()
    { }

    public ValueTask<TaskHubUsage> GetUsageAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult<TaskHubUsage>(default);
}
