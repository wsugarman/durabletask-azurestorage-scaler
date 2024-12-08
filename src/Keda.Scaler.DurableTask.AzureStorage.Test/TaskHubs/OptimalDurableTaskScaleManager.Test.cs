// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.TaskHubs;
using Keda.Scaler.DurableTask.AzureStorage.Test.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHubs;

public sealed class OptimalDurableTaskScaleManagerTest : IDisposable
{
    private readonly ITaskHub _taskHub = Substitute.For<ITaskHub>();
    private readonly TaskHubOptions _options = new() { TaskHubName = "UnitTest" };
    private readonly ILoggerFactory _loggerFactory;
    private readonly MockOptimalScaleManager _scaleManager;

    public OptimalDurableTaskScaleManagerTest(ITestOutputHelper outputHelper)
    {
        IOptionsSnapshot<TaskHubOptions> _optionsSnapshot = Substitute.For<IOptionsSnapshot<TaskHubOptions>>();
        _ = _optionsSnapshot.Get(default).Returns(_options);
        _loggerFactory = XUnitLogger.CreateFactory(outputHelper);
        _scaleManager = new MockOptimalScaleManager(_taskHub, _optionsSnapshot, _loggerFactory);
    }

    public void Dispose()
        => _loggerFactory.Dispose();

    [Theory]
    [InlineData(0, 3)]
    [InlineData(0, 1, 0, 0, 0)]
    [InlineData(2, 6, 1, 2, 3, 4)]
    [InlineData(2, 4, 3, 2, 1, 2)]
    [InlineData(7, 1, 5, 5, 5, 5, 5, 5, 5)]
    public void GivenPartitions_WhenGettingWorkerCount_ThenComputeOptimalNumber(int expected, int maxOrchestrationsPerWorker, params int[] partitions)
    {
        _options.MaxOrchestrationsPerWorker = maxOrchestrationsPerWorker;
        Assert.Equal(expected, _scaleManager.GetRequiredWorkerCount(new TaskHubQueueUsage(partitions, default)));
    }

    private sealed class MockOptimalScaleManager(ITaskHub taskHub, IOptionsSnapshot<TaskHubOptions> optionsSnapshot, ILoggerFactory loggerFactory)
        : OptimalDurableTaskScaleManager(taskHub, optionsSnapshot, loggerFactory)
    {
        public int GetRequiredWorkerCount(TaskHubQueueUsage usage)
            => GetWorkerCount(usage);
    }
}
