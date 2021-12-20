// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Linq;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Keda.Scaler.DurableTask.AzureStorage.Common;
using Keda.Scaler.DurableTask.AzureStorage.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Extensions.Test
{
    [TestClass]
    public class IServiceCollectionExtensionsTest
    {
        [TestMethod]
        public void AddScaler()
        {
            Assert.ThrowsException<ArgumentNullException>(() => IServiceCollectionExtensions.AddScaler(null!));

            IServiceCollection services = new ServiceCollection().AddScaler();
            Assert.IsTrue(services.Any(x => x.ServiceType == typeof(IDurableTaskAzureStorageScaler)));
            Assert.IsTrue(services.Any(x => x.ServiceType == typeof(ITokenCredentialFactory)));
            Assert.IsTrue(services.Any(x => x.ServiceType == typeof(IEnvironment)));
        }
    }
}
