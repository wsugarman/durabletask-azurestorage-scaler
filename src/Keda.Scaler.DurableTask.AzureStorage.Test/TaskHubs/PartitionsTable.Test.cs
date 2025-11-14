// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.TaskHubs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHubs;

[TestClass]
public class PartitionsTableTest
{
    [TestMethod]
    public void GivenNullTaskHub_WhenGettingPartitionsTableName_ThenThrowArgumentException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => PartitionsTable.GetName(null!));

    [TestMethod]
    [DataRow("")]
    [DataRow("  \t  ")]
    public void GivenEmptyOrWhiteSpaceTaskHub_WhenGettingPartitionsTableName_ThenThrowArgumentException(string taskHub)
        => Assert.ThrowsExactly<ArgumentException>(() => PartitionsTable.GetName(taskHub));

    [TestMethod]
    [DataRow("fooPartitions", "foo")]
    [DataRow("BarPartitions", "Bar")]
    [DataRow("BAZPartitions", "BAZ")]
    public void GivenTaskHub_WhenGettingPartitionsTableName_ThenReturnExpectedValue(string expected, string taskHub)
        => Assert.AreEqual(expected, PartitionsTable.GetName(taskHub));
}
