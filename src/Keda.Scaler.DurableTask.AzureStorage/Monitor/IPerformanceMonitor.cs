// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace Keda.Scaler.DurableTask.AzureStorage.Monitor;

/// <summary>
///  Represents a type used to monitor durable task performance.
/// </summary>
internal interface IPerformanceMonitor
{
    /// <summary>
    /// Get heart beat of durable task.
    /// </summary>
    /// <returns>The heart beat.</returns>
    Task<PerformanceHeartbeat?> GetHeartbeatAsync();
}
