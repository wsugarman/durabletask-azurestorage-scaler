// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.TaskHubs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHubs;

[TestClass]
public class TaskHubQueueUsageTest
{
    [TestMethod]
    public void GivenNullControlQueueMessages_WhenCreatingTaskHubQueueUsage_ThenThrowArgumentNullException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => new TaskHubQueueUsage(null!, 3));

    [TestMethod]
    [DataRow(-1)]
    [DataRow(1, 2, -3)]
    public void GivenInvalidControlQueueMessageCount_WhenCreatingTaskHubQueueUsage_ThenThrowArgumentOutOfRangeException(params int[] controlQueueMessages)
        => Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TaskHubQueueUsage(controlQueueMessages, 3));

    [TestMethod]
    public void GivenInvalidWorkItemQueueMessageCount_WhenCreatingTaskHubQueueUsage_ThenThrowArgumentOutOfRangeException()
        => Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TaskHubQueueUsage([], -2));

    [TestMethod]
    [DataRow(false, 0)]
    [DataRow(false, 0, 0, 0)]
    [DataRow(true, 2)]
    [DataRow(true, 1, 0)]
    [DataRow(true, 0, 1)]
    [DataRow(true, 4, 1, 2, 3, 4)]
    public void GivenUsage_WhenQueryingActivity_ThenReturnCorrespondingValue(bool expected, int workItems, params int[] control)
        => Assert.AreEqual(expected, new TaskHubQueueUsage(control, workItems).HasActivity);
}
