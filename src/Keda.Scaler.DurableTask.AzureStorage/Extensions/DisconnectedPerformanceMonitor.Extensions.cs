// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using DurableTask.AzureStorage.Monitoring;

namespace Keda.Scaler.DurableTask.AzureStorage.Extensions
{
    internal static class DisconnectedPerformanceMonitorExtensions
    {
        public static Task<PerformanceHeartbeat> PulseAsync(this DisconnectedPerformanceMonitor performanceMonitor, int? workerCount = null)
        {
            if (performanceMonitor is null)
                throw new ArgumentNullException(nameof(performanceMonitor));

            return workerCount.HasValue ? performanceMonitor.PulseAsync(workerCount.GetValueOrDefault()) : performanceMonitor.PulseAsync();
        }
    }
}
