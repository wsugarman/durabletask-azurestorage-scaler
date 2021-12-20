// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Common.Test
{
    [TestClass]
    public class EnvironmentCacheTest
    {
        [TestMethod]
        public void CtorExceptions()
            => Assert.ThrowsException<ArgumentNullException>(() => new EnvironmentCache(null!));

        [TestMethod]
        public void GetEnvironmentVariable()
        {
            IEnvironment environment = new EnvironmentCache(CurrentEnvironment.Instance);

            // Setup environment
            string variable = Guid.NewGuid().ToString();
            string value = Guid.NewGuid().ToString();
            string? previous = Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable(variable, value, EnvironmentVariableTarget.Process);

            try
            {
                // Populate cache
                Assert.AreEqual(value, environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.Process));

                // Change value in environment
                string newValue = Guid.NewGuid().ToString();
                Environment.SetEnvironmentVariable(variable, newValue, EnvironmentVariableTarget.Process);
                Assert.AreEqual(newValue, Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.Process));

                // Ensure previous value was cached
                Assert.AreEqual(value, environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.Process));
            }
            finally
            {
                Environment.SetEnvironmentVariable(variable, previous, EnvironmentVariableTarget.Process);
            }
        }
    }
}
