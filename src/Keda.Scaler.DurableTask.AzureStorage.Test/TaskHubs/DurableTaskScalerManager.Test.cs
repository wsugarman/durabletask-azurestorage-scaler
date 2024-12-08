// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using System.Threading;
using Keda.Scaler.DurableTask.AzureStorage.TaskHubs;
using Keda.Scaler.DurableTask.AzureStorage.Test.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHubs;

public sealed class DurableTaskScalerManagerTest : IDisposable
{
    private readonly ITaskHub _taskHub = Substitute.For<ITaskHub>();
    private readonly IOptionsSnapshot<TaskHubOptions> _optionsSnapshot = Substitute.For<IOptionsSnapshot<TaskHubOptions>>();
    private readonly ILoggerFactory _loggerFactory;
    private readonly MockScaleManager _scaleManager;

    private const string TaskHubName = "UnitTest";
    private const int MaxActivitiesPerWorker = 5;

    public DurableTaskScalerManagerTest(ITestOutputHelper outputHelper)
    {
        TaskHubOptions options = new()
        {
            MaxActivitiesPerWorker = MaxActivitiesPerWorker,
            TaskHubName = TaskHubName,
        };

        _ = _optionsSnapshot.Get(default).Returns(options);
        _loggerFactory = XUnitLogger.CreateFactory(outputHelper);
        _scaleManager = Substitute.For<MockScaleManager>(_taskHub, _optionsSnapshot, _loggerFactory);
    }

    public void Dispose()
        => _loggerFactory.Dispose();

    [Fact]
    public void GivenNullTaskHub_WhenCreatingScalerManager_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new ExampleDurableTaskScaleManager(null!, _optionsSnapshot, _loggerFactory));

    [Fact]
    public void GivenNullOptionsSnapshot_WhenCreatingScalerManager_ThenThrowArgumentNullException()
    {
        _ = Assert.Throws<ArgumentNullException>(() => new ExampleDurableTaskScaleManager(_taskHub, null!, _loggerFactory));

        IOptionsSnapshot<TaskHubOptions> nullSnapshot = Substitute.For<IOptionsSnapshot<TaskHubOptions>>();
        _ = nullSnapshot.Get(default).Returns(default(TaskHubOptions));
        _ = Assert.Throws<ArgumentNullException>(() => new ExampleDurableTaskScaleManager(_taskHub, nullSnapshot, _loggerFactory));
    }

    [Fact]
    public void GivenNullLoggerFactory_WhenCreatingScalerManager_ThenThrowArgumentNullException()
    {
        _ = Assert.Throws<ArgumentNullException>(() => new ExampleDurableTaskScaleManager(_taskHub, _optionsSnapshot, null!));

        ILoggerFactory nullFactory = Substitute.For<ILoggerFactory>();
        _ = nullFactory.CreateLogger(default!).ReturnsForAnyArgs(default(ILogger));
        _ = Assert.Throws<ArgumentNullException>(() => new ExampleDurableTaskScaleManager(_taskHub, _optionsSnapshot, nullFactory));
    }

    [Fact]
    public void GivenScaleManager_WhenGettingMetricSpec_ThenReturnOptionValue()
    {
        MetricSpec actual = _scaleManager.KedaMetricSpec;
        Assert.Equal(DurableTaskScaleManager.MetricName, actual.MetricName);
        Assert.Equal(MaxActivitiesPerWorker, actual.TargetSize);
    }

    [Fact]
    public async Task GivenNoActivity_WhenGettingMetricValues_ThenReturnZero()
    {
        _ = _taskHub.GetUsageAsync(default).ReturnsForAnyArgs(TaskHubQueueUsage.None);

        using CancellationTokenSource cts = new();
        MetricValue actual = await _scaleManager.GetKedaMetricValueAsync(cts.Token);

        _ = await _taskHub.Received(1).GetUsageAsync(cts.Token);
        _ = _scaleManager.DidNotReceiveWithAnyArgs().GetRequiredWorkerCount(default!);
        Assert.Equal(DurableTaskScaleManager.MetricName, actual.MetricName);
        Assert.Equal(0, actual.MetricValue_);
    }

    [Fact]
    public async Task GivenActivity_WhenGettingMetricValues_ThenReturnDerivedValue()
    {
        TaskHubQueueUsage usage = new([1, 2, 3, 4], 2);
        _ = _taskHub.GetUsageAsync(default).ReturnsForAnyArgs(usage);
        _ = _scaleManager.GetRequiredWorkerCount(default!).ReturnsForAnyArgs(3);

        using CancellationTokenSource cts = new();
        MetricValue actual = await _scaleManager.GetKedaMetricValueAsync(cts.Token);

        _ = await _taskHub.Received(1).GetUsageAsync(cts.Token);
        _ = _scaleManager.Received(1).GetRequiredWorkerCount(usage);
        Assert.Equal(DurableTaskScaleManager.MetricName, actual.MetricName);
        Assert.Equal(2 + (3 * MaxActivitiesPerWorker), actual.MetricValue_);
    }

    [Fact]
    public async Task GivenNoActivity_WhenCheckingIfActive_ThenReturnFalse()
    {
        _ = _taskHub.GetUsageAsync(default).ReturnsForAnyArgs(TaskHubQueueUsage.None);

        using CancellationTokenSource cts = new();
        bool actual = await _scaleManager.IsActiveAsync(cts.Token);

        _ = await _taskHub.Received(1).GetUsageAsync(cts.Token);
        Assert.False(actual);
    }

    [Fact]
    public async Task GivenActivity_WhenCheckingIfActive_ThenReturnTrue()
    {
        TaskHubQueueUsage usage = new([1, 2, 3, 4], 2);
        _ = _taskHub.GetUsageAsync(default).ReturnsForAnyArgs(usage);
        _ = _scaleManager.GetRequiredWorkerCount(default!).ReturnsForAnyArgs(3);

        using CancellationTokenSource cts = new();
        bool actual = await _scaleManager.IsActiveAsync(cts.Token);

        _ = await _taskHub.Received(1).GetUsageAsync(cts.Token);
        Assert.True(actual);
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
