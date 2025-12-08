// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.TaskHubs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHubs;

[TestClass]
public class ControlQueueTest
{
    [TestMethod]
    public void GivenNullTaskHub_WhenGettingControlQueueName_ThenThrowArgumentException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => LeasesContainer.GetName(null!));

    [TestMethod]
    [DataRow("")]
    [DataRow("  \t  ")]
    public void GivenEmptyOrWhiteSpaceTaskHub_WhenGettingControlQueueName_ThenThrowArgumentException(string taskHub)
        => Assert.ThrowsExactly<ArgumentException>(() => LeasesContainer.GetName(taskHub));

    [TestMethod]
    [DataRow(-2)]
    [DataRow(19)]
    public void GivenInvalidPartition_WhenGettingControlQueueName_ThenThrowArgumentOutOfRangeException(int partition)
        => Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ControlQueue.GetName("foo", partition));

    [TestMethod]
    [DataRow("foo-control-00", "foo", 0)]
    [DataRow("bar-control-07", "Bar", 7)]
    [DataRow("baz-control-15", "BAZ", 15)]
    public void GivenTaskHub_WhenGettingControlQueueName_ThenReturnExpectedValue(string expected, string taskHub, int partition)
        => Assert.AreEqual(expected, ControlQueue.GetName(taskHub, partition));
}
