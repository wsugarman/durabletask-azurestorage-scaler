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
        {
            // We cannot dispose the TokenCredential object because of a bug in the library that
            // attempts to dispose of the underlying timer and token source, even if they weren't created.
            // Given that this library is deprecated, we'll skip disposal as we don't leverage automatic renewal and
            // instead wait for a new version of the Durable Task library that leverages the newer
            // Azure.Identity library for authentication.

            // _credential?.Dispose();
        }

        public Task<PerformanceHeartbeat> GetHeartbeatAsync(int? workerCount = null)
            => workerCount.HasValue ? _monitor.PulseAsync(workerCount.GetValueOrDefault()) : _monitor.PulseAsync();

        internal DisconnectedPerformanceMonitor ToDisconnectedPerformanceMonitor()
            => _monitor;
    }
}
