// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Common.Test
{
    [TestClass]
    public class CurrentEnvironmentTest
    {
        [TestMethod]
        public void GetEnvironmentVariable()
        {
            IProcessEnvironment environment = CurrentEnvironment.Instance;

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
}
