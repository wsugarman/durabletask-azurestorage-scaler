// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.Kubernetes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Kubernetes;

[TestClass]
public class KubernetesResourceTest
{
    [TestMethod]
    public void Ctor()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new KubernetesResource(null!));
        Assert.ThrowsException<ArgumentNullException>(() => new KubernetesResource(""));
        Assert.ThrowsException<ArgumentNullException>(() => new KubernetesResource("   "));
        Assert.ThrowsException<ArgumentNullException>(() => new KubernetesResource("func", null!));
        Assert.ThrowsException<ArgumentNullException>(() => new KubernetesResource("func", ""));
        Assert.ThrowsException<ArgumentNullException>(() => new KubernetesResource("func", "   "));

        KubernetesResource deployment;

        // Default namespace
        deployment = new KubernetesResource("func");
        Assert.AreEqual("func", deployment.Name);
        Assert.AreEqual("default", deployment.Namespace);

        // Specify both values
        deployment = new KubernetesResource("func", "unit-test-namespace");
        Assert.AreEqual("func", deployment.Name);
        Assert.AreEqual("unit-test-namespace", deployment.Namespace);
    }

    [TestMethod]
    public void EqualsObj()
    {
        Assert.IsFalse(new KubernetesResource("1").Equals(null!));
        Assert.IsFalse(new KubernetesResource("2").Equals("3"));
        AssertEquality((x, y) => x.Equals(y));
    }

    [TestMethod]
    public void Equals()
        => AssertEquality((x, y) => x.Equals(y));

    [TestMethod]
    public void GetHashCodeOverride()
        => AssertEquality((x, y) => x.GetHashCode() == y.GetHashCode());

    [TestMethod]
    public void Equality()
        => AssertEquality((x, y) => x == y);

    [TestMethod]
    public void Inequality()
        => AssertEquality((x, y) => !(x != y));

    private static void AssertEquality(Func<KubernetesResource, KubernetesResource, bool> comparison)
    {
        Assert.IsTrue(comparison(new KubernetesResource("A"), new KubernetesResource("A")));
        Assert.IsTrue(comparison(new KubernetesResource("B", "unit-test"), new KubernetesResource("B", "unit-test")));

        Assert.IsFalse(comparison(new KubernetesResource("C"), new KubernetesResource("D")));
        Assert.IsFalse(comparison(new KubernetesResource("E", "unit-test-1"), new KubernetesResource("E", "unit-test-2")));
        Assert.IsFalse(comparison(new KubernetesResource("F", "unit-test-3"), new KubernetesResource("G", "unit-test-4")));
    }
}
