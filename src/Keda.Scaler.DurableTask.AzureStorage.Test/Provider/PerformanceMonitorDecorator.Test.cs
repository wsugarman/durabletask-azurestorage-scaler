// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using DurableTask.AzureStorage.Monitoring;
using Keda.Scaler.DurableTask.AzureStorage.Provider;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Auth;
using Moq;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Provider;

[TestClass]
public class PerformanceMonitorDecoratorTest
{
    [TestMethod]
    public void CtorExceptions()
        => Assert.ThrowsException<ArgumentNullException>(() => new PerformanceMonitorDecorator(null!));

    [TestMethod]
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "TokenCredential has a bug.")]
    public void Dispose()
    {
        Mock<DisconnectedPerformanceMonitor> mock = new Mock<DisconnectedPerformanceMonitor>(
            MockBehavior.Strict,
            "UseDevelopmentStorage=true",
            "UnitTestTaskHub");

        new PerformanceMonitorDecorator(mock.Object).Dispose();
        new PerformanceMonitorDecorator(mock.Object, new TokenCredential("ABC")).Dispose();
    }

    [TestMethod]
    public async Task GetHeartbeatAsync()
    {
        const int workerCount = 12;

        int p0Calls = 0, p1Calls = 0;
        PerformanceHeartbeat p0Heartbeat = new PerformanceHeartbeat(), p1Heartbeat = new PerformanceHeartbeat();

        Mock<DisconnectedPerformanceMonitor> mock = new Mock<DisconnectedPerformanceMonitor>(
            MockBehavior.Strict,
            "UseDevelopmentStorage=true",
            "UnitTestTaskHub");
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
