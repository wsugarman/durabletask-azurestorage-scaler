// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.TaskHubs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHubs;

[TestClass]
public class LeasesContainerTest
{
    [TestMethod]
    public void GivenNullTaskHub_WhenGettingLeasesContainerName_ThenThrowArgumentException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => LeasesContainer.GetName(null!));

    [TestMethod]
    [DataRow("")]
    [DataRow("  \t  ")]
    public void GivenEmptyOrWhiteSpaceTaskHub_WhenGettingLeasesContainerName_ThenThrowArgumentException(string taskHub)
        => Assert.ThrowsExactly<ArgumentException>(() => LeasesContainer.GetName(taskHub));

    [TestMethod]
    [DataRow("foo-leases", "foo")]
    [DataRow("bar-leases", "bar")]
    [DataRow("baz-leases", "BAZ")]
    public void GivenTaskHub_WhenGettingLeasesContainerName_ThenReturnExpectedValue(string expected, string taskHub)
        => Assert.AreEqual(expected, LeasesContainer.GetName(taskHub));
}
