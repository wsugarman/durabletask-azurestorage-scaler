// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.Queues;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Queues;

[TestClass]
public class ControlQueueTest
{
    [TestMethod]
    public void GetName()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => ControlQueue.GetName("foo", -2));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => ControlQueue.GetName("foo", 19));

        Assert.AreEqual("-control-01", ControlQueue.GetName(null, 1));
        Assert.AreEqual("foo-control-00", ControlQueue.GetName("foo", 0));
        Assert.AreEqual("bar-control-03", ControlQueue.GetName("bar", 3));
        Assert.AreEqual("baz-control-12", ControlQueue.GetName("baz", 12));
        Assert.AreEqual("an-other-control-15", ControlQueue.GetName("an-OTHer", 15));
    }
}
