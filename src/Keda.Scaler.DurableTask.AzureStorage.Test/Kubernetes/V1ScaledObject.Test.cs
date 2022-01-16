// Copyright © William Sugarman.
// Licensed under the MIT License.

using k8s.Models;
using Keda.Scaler.DurableTask.AzureStorage.Kubernetes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Kubernetes;

[TestClass]
public class V1ScaledObjectTest
{
    [TestMethod]
    public void Validate()
    {
        V1ScaledObject obj = new V1ScaledObject();

        // Default value
        obj.Validate();

        // With properties
        obj.Metadata = new V1ObjectMeta();
        obj.Spec = new V1ScaledObjectSpec { ScaleTargetRef = new V1ScaleTargetRef { Name = "test" } };
        obj.Validate();
    }
}
