// Copyright © William Sugarman.
// Licensed under the MIT License.

using Keda.Scaler.DurableTask.AzureStorage.TaskHubs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHubs;

[TestClass]
public sealed class OptimalDurableTaskScaleManagerTest
{
    private readonly ITaskHub _taskHub = Substitute.For<ITaskHub>();
    private readonly TaskHubOptions _options = new() { TaskHubName = "UnitTest" };
    private readonly MockOptimalScaleManager _scaleManager;

    public OptimalDurableTaskScaleManagerTest()
    {
        IOptionsSnapshot<TaskHubOptions> _optionsSnapshot = Substitute.For<IOptionsSnapshot<TaskHubOptions>>();
        _ = _optionsSnapshot.Get(default).Returns(_options);
        _scaleManager = new MockOptimalScaleManager(_taskHub, _optionsSnapshot, NullLoggerFactory.Instance);
    }

    [TestMethod]
    [DataRow(0, 3)]
    [DataRow(0, 1, 0, 0, 0)]
    [DataRow(2, 6, 1, 2, 3, 4)]
    [DataRow(2, 4, 3, 2, 1, 2)]
    [DataRow(7, 1, 5, 5, 5, 5, 5, 5, 5)]
    public void GivenPartitions_WhenGettingWorkerCount_ThenComputeOptimalNumber(int expected, int maxOrchestrationsPerWorker, params int[] partitions)
    {
        _options.MaxOrchestrationsPerWorker = maxOrchestrationsPerWorker;
        Assert.AreEqual(expected, _scaleManager.GetRequiredWorkerCount(new TaskHubQueueUsage(partitions, default)));
    }

    private sealed class MockOptimalScaleManager(ITaskHub taskHub, IOptionsSnapshot<TaskHubOptions> optionsSnapshot, ILoggerFactory loggerFactory)
        : OptimalDurableTaskScaleManager(taskHub, optionsSnapshot, loggerFactory)
    {
        public int GetRequiredWorkerCount(TaskHubQueueUsage usage)
            => GetWorkerCount(usage);
    }
}
