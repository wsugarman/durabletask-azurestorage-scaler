// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.TaskHub;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHub;

[TestClass]
public class OptimalOrchestrationAllocatorTest
{
    private readonly OptimalOrchestrationAllocator _allocator = new OptimalOrchestrationAllocator();

    [TestMethod]
    public void GetWorkerCount()
    {
        // Exceptions
        Assert.ThrowsException<ArgumentNullException>(() => _allocator.GetWorkerCount(null!, 5));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => _allocator.GetWorkerCount(Array.Empty<int>(), -3));

        // Valid cases
        Assert.AreEqual(0, _allocator.GetWorkerCount(Array.Empty<int>(), 3));
        Assert.AreEqual(0, _allocator.GetWorkerCount(new int[] { 0, 0, 0 }, 1));
        Assert.AreEqual(2, _allocator.GetWorkerCount(new int[] { 1, 2, 3, 4 }, 6));
        Assert.AreEqual(2, _allocator.GetWorkerCount(new int[] { 3, 2, 1, 2 }, 4));
        Assert.AreEqual(7, _allocator.GetWorkerCount(new int[] { 5, 5, 5, 5, 5, 5, 5 }, 1));
    }
}
