// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using DurableTask.AzureStorage.Monitoring;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Keda.Scaler.DurableTask.AzureStorage.Extensions.Test
{
    [TestClass]
    public class DisconnectedPerformanceMonitorExtensionsTest
    {
        [TestMethod]
        public async Task PulseAsync()
        {
            const int workerCount = 12;

            int p0Calls = 0, p1Calls = 0;
            PerformanceHeartbeat p0Heartbeat = new PerformanceHeartbeat(), p1Heartbeat = new PerformanceHeartbeat();

            Mock<DisconnectedPerformanceMonitor> mock = new Mock<DisconnectedPerformanceMonitor>(MockBehavior.Strict);
            mock.Setup(m => m.PulseAsync()).Callback(() => p0Calls++).ReturnsAsync(p0Heartbeat);
            mock.Setup(m => m.PulseAsync(workerCount)).Callback(() => p1Calls++).ReturnsAsync(p1Heartbeat);

            DisconnectedPerformanceMonitor performanceMonitor = mock.Object;
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => DisconnectedPerformanceMonitorExtensions.PulseAsync(null!, workerCount)).ConfigureAwait(false);

            Assert.AreSame(p0Heartbeat, await performanceMonitor.PulseAsync(null).ConfigureAwait(false));
            Assert.AreEqual(1, p0Calls);
            Assert.AreEqual(0, p1Calls);

            Assert.AreSame(p1Heartbeat, await performanceMonitor.PulseAsync((int?)workerCount).ConfigureAwait(false));
            Assert.AreEqual(1, p0Calls);
            Assert.AreEqual(1, p1Calls);
        }
    }
}
