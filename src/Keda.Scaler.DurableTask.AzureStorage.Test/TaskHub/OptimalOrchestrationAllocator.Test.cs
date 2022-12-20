// Copyright © William Sugarman.
// Licensed under the MIT License.

using Keda.Scaler.DurableTask.AzureStorage.TaskHub;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHub;

public class OptimalOrchestrationAllocatorTest
{
    private readonly OptimalOrchestrationAllocator _allocator = new OptimalOrchestrationAllocator();

    [DataTestMethod]
    [DataRow(0, 3)]
    [DataRow(0, 1, 0, 0, 0)]
    [DataRow(2, 4, 3, 2, 1, 2)]
    [DataRow(7, 1, 5, 5, 5, 5, 5, 5, 5)]
    public void GetWorkerCount(int expected, int maxPerWorker, params int[] counts)
        => Assert.AreEqual(expected, _allocator.GetWorkerCount(counts, maxPerWorker));
}
