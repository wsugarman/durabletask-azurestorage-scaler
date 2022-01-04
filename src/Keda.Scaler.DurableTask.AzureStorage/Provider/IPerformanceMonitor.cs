// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using DurableTask.AzureStorage.Monitoring;

namespace Keda.Scaler.DurableTask.AzureStorage.Provider;

internal interface IPerformanceMonitor : IDisposable
{
    Task<PerformanceHeartbeat?> GetHeartbeatAsync(int? workerCount = null);
}
