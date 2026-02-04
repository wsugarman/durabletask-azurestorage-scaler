// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Keda.Scaler.DurableTask.AzureStorage.TaskHubs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHubs;

[TestClass]
public sealed class DurableTaskScalerManagerTest
{
    private readonly TestContext _testContext;
    private readonly ITaskHub _taskHub;
    private readonly IOptionsSnapshot<TaskHubOptions> _optionsSnapshot;
    private readonly MockScaleManager _scaleManager;

    private const string TaskHubName = "UnitTest";
    private const int MaxActivitiesPerWorker = 5;

    public DurableTaskScalerManagerTest(TestContext testContext)
    {
        ArgumentNullException.ThrowIfNull(testContext);

        _testContext = testContext;
        _taskHub = Substitute.For<ITaskHub>();
        _optionsSnapshot = Substitute.For<IOptionsSnapshot<TaskHubOptions>>();

        TaskHubOptions options = new()
        {
            MaxActivitiesPerWorker = MaxActivitiesPerWorker,
            TaskHubName = TaskHubName,
        };

        _ = _optionsSnapshot.Get(default).Returns(options);
        _scaleManager = Substitute.For<MockScaleManager>(_taskHub, _optionsSnapshot, NullLoggerFactory.Instance);
    }

    [TestMethod]
    public void GivenNullTaskHub_WhenCreatingScalerManager_ThenThrowArgumentNullException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => new ExampleDurableTaskScaleManager(null!, _optionsSnapshot, NullLoggerFactory.Instance));

    [TestMethod]
    public void GivenNullOptionsSnapshot_WhenCreatingScalerManager_ThenThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ExampleDurableTaskScaleManager(_taskHub, null!, NullLoggerFactory.Instance));

        IOptionsSnapshot<TaskHubOptions> nullSnapshot = Substitute.For<IOptionsSnapshot<TaskHubOptions>>();
        _ = nullSnapshot.Get(default).Returns(default(TaskHubOptions));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ExampleDurableTaskScaleManager(_taskHub, nullSnapshot, NullLoggerFactory.Instance));
    }

    [TestMethod]
    public void GivenNullLoggerFactory_WhenCreatingScalerManager_ThenThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ExampleDurableTaskScaleManager(_taskHub, _optionsSnapshot, null!));

        ILoggerFactory nullFactory = Substitute.For<ILoggerFactory>();
        _ = nullFactory.CreateLogger(default!).ReturnsForAnyArgs(default(ILogger));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ExampleDurableTaskScaleManager(_taskHub, _optionsSnapshot, nullFactory));
    }

    [TestMethod]
    public void GivenScaleManager_WhenGettingMetricSpec_ThenReturnOptionValue()
    {
        MetricSpec actual = _scaleManager.KedaMetricSpec;
        Assert.AreEqual(DurableTaskScaleManager.MetricName, actual.MetricName);
        Assert.AreEqual(MaxActivitiesPerWorker, actual.TargetSize);
    }

    [TestMethod]
    public async ValueTask GivenNoActivity_WhenGettingMetricValues_ThenReturnZero()
    {
        _ = _taskHub.GetUsageAsync(_testContext.CancellationToken).ReturnsForAnyArgs(TaskHubQueueUsage.None);

        using CancellationTokenSource cts = new();
        MetricValue actual = await _scaleManager.GetKedaMetricValueAsync(cts.Token);

        _ = await _taskHub.Received(1).GetUsageAsync(cts.Token);
        _ = _scaleManager.DidNotReceiveWithAnyArgs().GetRequiredWorkerCount(default!);
        Assert.AreEqual(DurableTaskScaleManager.MetricName, actual.MetricName);
        Assert.AreEqual(0, actual.MetricValue_);
    }

    [TestMethod]
    public async ValueTask GivenActivity_WhenGettingMetricValues_ThenReturnDerivedValue()
    {
        TaskHubQueueUsage usage = new([1, 2, 3, 4], 2);
        _ = _taskHub.GetUsageAsync(_testContext.CancellationToken).ReturnsForAnyArgs(usage);
        _ = _scaleManager.GetRequiredWorkerCount(default!).ReturnsForAnyArgs(3);

        using CancellationTokenSource cts = new();
        MetricValue actual = await _scaleManager.GetKedaMetricValueAsync(cts.Token);

        _ = await _taskHub.Received(1).GetUsageAsync(cts.Token);
        _ = _scaleManager.Received(1).GetRequiredWorkerCount(usage);
        Assert.AreEqual(DurableTaskScaleManager.MetricName, actual.MetricName);
        Assert.AreEqual(2 + (3 * MaxActivitiesPerWorker), actual.MetricValue_);
    }

    [TestMethod]
    public async ValueTask GivenNoActivity_WhenCheckingIfActive_ThenReturnFalse()
    {
        _ = _taskHub.GetUsageAsync(_testContext.CancellationToken).ReturnsForAnyArgs(TaskHubQueueUsage.None);

        using CancellationTokenSource cts = new();
        bool actual = await _scaleManager.IsActiveAsync(cts.Token);

        _ = await _taskHub.Received(1).GetUsageAsync(cts.Token);
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public async ValueTask GivenActivity_WhenCheckingIfActive_ThenReturnTrue()
    {
        TaskHubQueueUsage usage = new([1, 2, 3, 4], 2);
        _ = _taskHub.GetUsageAsync(_testContext.CancellationToken).ReturnsForAnyArgs(usage);
        _ = _scaleManager.GetRequiredWorkerCount(default!).ReturnsForAnyArgs(3);

        using CancellationTokenSource cts = new();
        bool actual = await _scaleManager.IsActiveAsync(cts.Token);

        _ = await _taskHub.Received(1).GetUsageAsync(cts.Token);
        Assert.IsTrue(actual);
    }

    private sealed class ExampleDurableTaskScaleManager(ITaskHub taskHub, IOptionsSnapshot<TaskHubOptions> optionsSnapshot, ILoggerFactory loggerFactory)
        : DurableTaskScaleManager(taskHub, optionsSnapshot, loggerFactory)
    {
        protected override int GetWorkerCount(TaskHubQueueUsage usage)
            => default;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Type must be public for mocking.")]
    public abstract class MockScaleManager(ITaskHub taskHub, IOptionsSnapshot<TaskHubOptions> optionsSnapshot, ILoggerFactory loggerFactory)
        : DurableTaskScaleManager(taskHub, optionsSnapshot, loggerFactory)
    {
        public sealed override MetricSpec KedaMetricSpec => base.KedaMetricSpec;

        public sealed override ValueTask<MetricValue> GetKedaMetricValueAsync(CancellationToken cancellationToken = default)
            => base.GetKedaMetricValueAsync(cancellationToken);

        public sealed override ValueTask<bool> IsActiveAsync(CancellationToken cancellationToken = default)
            => base.IsActiveAsync(cancellationToken);

        public abstract int GetRequiredWorkerCount(TaskHubQueueUsage usage);

        protected sealed override int GetWorkerCount(TaskHubQueueUsage usage)
            => GetRequiredWorkerCount(usage);
    }
}
