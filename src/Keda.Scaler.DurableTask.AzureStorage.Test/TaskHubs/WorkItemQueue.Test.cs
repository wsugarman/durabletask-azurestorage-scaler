// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.TaskHubs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHubs;

[TestClass]
public class WorkItemQueueTest
{
    [TestMethod]
    public void GivenNullTaskHub_WhenGettingWorkItemQueueName_ThenThrowArgumentException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => WorkItemQueue.GetName(null!));

    [TestMethod]
    [DataRow("")]
    [DataRow("  \t  ")]
    public void GivenEmptyOrWhiteSpaceTaskHub_WhenGettingWorkItemQueueName_ThenThrowArgumentException(string taskHub)
        => Assert.ThrowsExactly<ArgumentException>(() => WorkItemQueue.GetName(taskHub));

    [TestMethod]
    [DataRow("foo-workitems", "foo")]
    [DataRow("bar-workitems", "bar")]
    [DataRow("baz-workitems", "BAZ")]
    public void GivenTaskHub_WhenGettingWorkItemQueueName_ThenReturnExpectedValue(string expected, string taskHub)
        => Assert.AreEqual(expected, WorkItemQueue.GetName(taskHub));
}
