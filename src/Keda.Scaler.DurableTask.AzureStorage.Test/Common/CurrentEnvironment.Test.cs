// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Common;

[TestClass]
public class CurrentEnvironmentTest
{
    [TestMethod]
    public void GetEnvironmentVariable()
    {
        IProcessEnvironment environment = ProcessEnvironment.Current;

        // Setup environment
        string variable = Guid.NewGuid().ToString();
        string value = Guid.NewGuid().ToString();
        string? previous = Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable(variable, value, EnvironmentVariableTarget.Process);

        try
        {
            // Fetch variable
            Assert.AreEqual(value, environment.GetEnvironmentVariable(variable));
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, previous, EnvironmentVariableTarget.Process);
        }
    }
}
