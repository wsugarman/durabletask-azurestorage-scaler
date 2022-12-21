// Copyright © William Sugarman.
// Licensed under the MIT License.

using Keda.Scaler.DurableTask.AzureStorage.Queues;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Queues;

[TestClass]
public class WorkItemQueueTest
{
    [TestMethod]
    public void GetName()
    {
        Assert.AreEqual("-workitems", WorkItemQueue.GetName(null));
        Assert.AreEqual("foo-workitems", WorkItemQueue.GetName("foo"));
    }
}
