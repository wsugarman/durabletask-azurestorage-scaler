// Copyright © William Sugarman.
// Licensed under the MIT License.

using Keda.Scaler.DurableTask.AzureStorage.Blobs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Blobs;

[TestClass]
public class LeasesContainerTest
{
    [TestMethod]
    public void GetName()
    {
        Assert.AreEqual("-leases", LeasesContainer.GetName(null));
        Assert.AreEqual("foo-leases", LeasesContainer.GetName("foo"));
        Assert.AreEqual("bar-baz-leases", LeasesContainer.GetName("BaR-bAz"));
    }
}
