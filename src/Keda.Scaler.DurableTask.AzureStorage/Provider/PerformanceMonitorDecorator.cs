// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using DurableTask.AzureStorage.Monitoring;
using Microsoft.WindowsAzure.Storage.Auth;

namespace Keda.Scaler.DurableTask.AzureStorage.Provider
{
    internal sealed class PerformanceMonitorDecorator : IPerformanceMonitor
    {
        internal bool HasTokenCredential => _credential is not null;

        private readonly TokenCredential? _credential;
        private readonly DisconnectedPerformanceMonitor _monitor;

        public PerformanceMonitorDecorator(DisconnectedPerformanceMonitor monitor, TokenCredential? credential = null)
        {
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            _credential = credential;
        }

        public void Dispose()
            => _credential?.Dispose(); // In truth, disposal isn't necessary because we don't renew it via the timer

        public Task<PerformanceHeartbeat> GetHeartbeatAsync(int? workerCount = null)
            => workerCount.HasValue ? _monitor.PulseAsync(workerCount.GetValueOrDefault()) : _monitor.PulseAsync();

        internal DisconnectedPerformanceMonitor ToDisconnectedPerformanceMonitor()
            => _monitor;
    }
}
