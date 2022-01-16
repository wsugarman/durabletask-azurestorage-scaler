// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.Kubernetes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Kubernetes;

[TestClass]
public class V1ScaledObjectSpecTest
{
    [TestMethod]
    public void Validate()
    {
        V1ScaledObjectSpec spec = new V1ScaledObjectSpec();

        // No ScaleTargetRef
        Assert.ThrowsException<ArgumentNullException>(() => spec.Validate());

        // Valid ScaleTargetRef
        spec.ScaleTargetRef = new V1ScaleTargetRef { Name = "test" };
        spec.Validate();
    }
}
