// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using DurableTask.AzureStorage.Monitoring;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Auth;
using Moq;

namespace Keda.Scaler.DurableTask.AzureStorage.Provider.Test
{
    [TestClass]
    public class PerformanceMonitorDecoratorTest
    {
        [TestMethod]
        public void CtorExceptions()
            => Assert.ThrowsException<ArgumentNullException>(() => new PerformanceMonitorDecorator(null!));

        [TestMethod]
        public async Task Dispose()
        {
            // Don't raise an exception if no credential is present
            new PerformanceMonitorDecorator(Mock.Of<DisconnectedPerformanceMonitor>()).Dispose();

            // Ensure credential is disposed
            // Note: We have no way to mock the TokenCredential or check for disposal, so we'll check via the callback
            int renewals = 0;
            using TokenCredential credential = new TokenCredential(
                Guid.NewGuid().ToString(),
                (s, t) =>
                {
                    renewals++;
                    return Task.FromResult(new NewTokenAndFrequency(Guid.NewGuid().ToString(), TimeSpan.Zero));
                },
                null,
                TimeSpan.Zero);

            PerformanceMonitorDecorator monitor = new PerformanceMonitorDecorator(
                Mock.Of<DisconnectedPerformanceMonitor>(),
                credential);

            monitor.Dispose();

            await Task.Delay(500).ConfigureAwait(false);
            int finalCount = renewals;
            await Task.Delay(500).ConfigureAwait(false);
            Assert.AreEqual(finalCount, renewals);
        }

        [TestMethod]
        public async Task GetHeartbeatAsync()
        {
            const int workerCount = 12;

            int p0Calls = 0, p1Calls = 0;
            PerformanceHeartbeat p0Heartbeat = new PerformanceHeartbeat(), p1Heartbeat = new PerformanceHeartbeat();

            Mock<DisconnectedPerformanceMonitor> mock = new Mock<DisconnectedPerformanceMonitor>(MockBehavior.Strict);
            mock.Setup(m => m.PulseAsync()).Callback(() => p0Calls++).ReturnsAsync(p0Heartbeat);
            mock.Setup(m => m.PulseAsync(workerCount)).Callback(() => p1Calls++).ReturnsAsync(p1Heartbeat);

            using PerformanceMonitorDecorator monitor = new PerformanceMonitorDecorator(mock.Object);

            Assert.AreSame(p0Heartbeat, await monitor.GetHeartbeatAsync(null).ConfigureAwait(false));
            Assert.AreEqual(1, p0Calls);
            Assert.AreEqual(0, p1Calls);

            Assert.AreSame(p1Heartbeat, await monitor.GetHeartbeatAsync(workerCount).ConfigureAwait(false));
            Assert.AreEqual(1, p0Calls);
            Assert.AreEqual(1, p1Calls);
        }
    }
}
