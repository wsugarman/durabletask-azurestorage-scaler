// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test;

[TestClass]
public class DeploymentReferenceTest
{
    [TestMethod]
    public void Ctor()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new DeploymentReference(null!));
        Assert.ThrowsException<ArgumentNullException>(() => new DeploymentReference(""));
        Assert.ThrowsException<ArgumentNullException>(() => new DeploymentReference("   "));
        Assert.ThrowsException<ArgumentNullException>(() => new DeploymentReference("func", null!));
        Assert.ThrowsException<ArgumentNullException>(() => new DeploymentReference("func", ""));
        Assert.ThrowsException<ArgumentNullException>(() => new DeploymentReference("func", "   "));

        DeploymentReference deployment;

        // Default namespace
        deployment = new DeploymentReference("func");
        Assert.AreEqual("func", deployment.Name);
        Assert.AreEqual("default", deployment.Namespace);

        // Specify both values
        deployment = new DeploymentReference("func", "unit-test-namespace");
        Assert.AreEqual("func", deployment.Name);
        Assert.AreEqual("unit-test-namespace", deployment.Namespace);
    }

    [TestMethod]
    public void EqualsObj()
    {
        Assert.IsFalse(new DeploymentReference("1").Equals(null!));
        Assert.IsFalse(new DeploymentReference("2").Equals("3"));
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

    private static void AssertEquality(Func<DeploymentReference, DeploymentReference, bool> comparison)
    {
        Assert.IsTrue(comparison(new DeploymentReference("A"), new DeploymentReference("A")));
        Assert.IsTrue(comparison(new DeploymentReference("B", "unit-test"), new DeploymentReference("B", "unit-test")));

        Assert.IsFalse(comparison(new DeploymentReference("C"), new DeploymentReference("D")));
        Assert.IsFalse(comparison(new DeploymentReference("E", "unit-test-1"), new DeploymentReference("E", "unit-test-2")));
        Assert.IsFalse(comparison(new DeploymentReference("F", "unit-test-3"), new DeploymentReference("G", "unit-test-4")));
    }
}
