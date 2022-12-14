// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHub;

internal interface ITaskHubMonitor
{
    ValueTask<TaskHubUsage> GetUsageAsync(CancellationToken cancellationToken = default);
}
