// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.Kubernetes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Kubernetes;

[TestClass]
public class V1ScaleTargetRefTest
{
    [TestMethod]
    public void Validate()
    {
        V1ScaleTargetRef scaleTarget = new V1ScaleTargetRef();

        // No Name
        Assert.ThrowsException<ArgumentNullException>(() => scaleTarget.Validate());

        // No ApiVersion
        scaleTarget.Name = "test";
        scaleTarget.Validate();

        // Invalid ApiVersion
        scaleTarget.ApiVersion = "bad";
        Assert.ThrowsException<ArgumentException>(() => scaleTarget.Validate());

        // Valid ApiVersion
        scaleTarget.ApiVersion = "unit/test";
        scaleTarget.Validate();
    }
}
