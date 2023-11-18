// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Common;

[TestClass]
public class EnvironmentCacheTest
{
    [TestMethod]
    public void CtorExceptions()
        => Assert.ThrowsException<ArgumentNullException>(() => new EnvironmentCache(null!));

    [TestMethod]
    public void GetEnvironmentVariable()
    {
        MockEnvironment env = new();
        EnvironmentCache cache = new(env);

        env.SetEnvironmentVariable("2", "two");
        Assert.AreEqual("two", cache.GetEnvironmentVariable("2"));

        env.SetEnvironmentVariable("2", "deux");
        Assert.AreEqual("two", cache.GetEnvironmentVariable("2"));
    }
}
