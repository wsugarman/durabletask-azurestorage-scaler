// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.TaskHub;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHub;

public class OptimalOrchestrationAllocatorTest
{
    private readonly OptimalOrchestrationAllocator _allocator = new();

    [Fact]
    public void GivenNullPartitions_WhenGettingWorkerCount_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => _allocator.GetWorkerCount(null!, 5));

    [Fact]
    public void GivenInvalidMaxWorkItems_WhenGettingWorkerCount_ThenThrowArgumentOutOfRangeException()
        => Assert.Throws<ArgumentOutOfRangeException>(() => _allocator.GetWorkerCount([], -3));

    [Theory]
    [InlineData(0, 3)]
    [InlineData(0, 1, 0, 0, 0)]
    [InlineData(2, 6, 1, 2, 3, 4)]
    [InlineData(2, 4, 3, 2, 1, 2)]
    [InlineData(7, 1, 5, 5, 5, 5, 5, 5, 5)]
    public void GivenPartitions_WhenGettingWorkerCount_ThenComputeOptimalNumber(int expected, int maxWorkItems, params int[] partitions)
        => Assert.Equal(expected, _allocator.GetWorkerCount(partitions, maxWorkItems));
}
