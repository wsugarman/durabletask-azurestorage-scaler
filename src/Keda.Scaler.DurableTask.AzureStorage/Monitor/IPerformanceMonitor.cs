// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace Keda.Scaler.DurableTask.AzureStorage.Monitor;

internal interface IPerformanceMonitor
{
    Task<PerformanceHeartbeat?> GetHeartbeatAsync();
}
