// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.Kubernetes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Kubernetes;

[TestClass]
public class ScaledObjectReferenceTest
{
    [TestMethod]
    public void Ctor()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new ScaledObjectReference(null!));
        Assert.ThrowsException<ArgumentNullException>(() => new ScaledObjectReference(""));
        Assert.ThrowsException<ArgumentNullException>(() => new ScaledObjectReference("   "));
        Assert.ThrowsException<ArgumentNullException>(() => new ScaledObjectReference("func", null!));
        Assert.ThrowsException<ArgumentNullException>(() => new ScaledObjectReference("func", ""));
        Assert.ThrowsException<ArgumentNullException>(() => new ScaledObjectReference("func", "   "));

        ScaledObjectReference deployment;

        // Default namespace
        deployment = new ScaledObjectReference("func");
        Assert.AreEqual("func", deployment.Name);
        Assert.AreEqual("default", deployment.Namespace);

        // Specify both values
        deployment = new ScaledObjectReference("func", "unit-test-namespace");
        Assert.AreEqual("func", deployment.Name);
        Assert.AreEqual("unit-test-namespace", deployment.Namespace);
    }

    [TestMethod]
    public void EqualsObj()
    {
        Assert.IsFalse(new ScaledObjectReference("1").Equals(null!));
        Assert.IsFalse(new ScaledObjectReference("2").Equals("3"));
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

    private static void AssertEquality(Func<ScaledObjectReference, ScaledObjectReference, bool> comparison)
    {
        Assert.IsTrue(comparison(new ScaledObjectReference("A"), new ScaledObjectReference("A")));
        Assert.IsTrue(comparison(new ScaledObjectReference("B", "unit-test"), new ScaledObjectReference("B", "unit-test")));

        Assert.IsFalse(comparison(new ScaledObjectReference("C"), new ScaledObjectReference("D")));
        Assert.IsFalse(comparison(new ScaledObjectReference("E", "unit-test-1"), new ScaledObjectReference("E", "unit-test-2")));
        Assert.IsFalse(comparison(new ScaledObjectReference("F", "unit-test-3"), new ScaledObjectReference("G", "unit-test-4")));
    }
}
