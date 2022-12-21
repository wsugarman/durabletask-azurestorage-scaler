// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Linq;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Keda.Scaler.DurableTask.AzureStorage.Accounts;
using Keda.Scaler.DurableTask.AzureStorage.Common;
using Keda.Scaler.DurableTask.AzureStorage.TaskHub;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Extensions;

[TestClass]
public class IServiceCollectionExtensionsTest
{
    [TestMethod]
    public void AddScaler()
    {
        Assert.ThrowsException<ArgumentNullException>(() => IServiceCollectionExtensions.AddDurableTaskScaler(null!));

        IServiceCollection services = new ServiceCollection().AddDurableTaskScaler();
        Assert.AreEqual(5, services.Count);

        // Singletons
        Assert.AreEqual(ServiceLifetime.Singleton, services.Single(x =>
            x.ServiceType == typeof(IStorageAccountClientFactory<BlobServiceClient>) &&
            x.ImplementationType == typeof(BlobServiceClientFactory)).Lifetime);
        Assert.AreEqual(ServiceLifetime.Singleton, services.Single(x =>
            x.ServiceType == typeof(IStorageAccountClientFactory<QueueServiceClient>) &&
            x.ImplementationType == typeof(QueueServiceClientFactory)).Lifetime);
        Assert.AreEqual(ServiceLifetime.Singleton, services.Single(x =>
            x.ServiceType == typeof(IOrchestrationAllocator) &&
            x.ImplementationType == typeof(OptimalOrchestrationAllocator)).Lifetime);

        // Scoped
        Assert.AreEqual(ServiceLifetime.Scoped, services.Single(x => x.ServiceType == typeof(IProcessEnvironment)).Lifetime);
        Assert.AreEqual(ServiceLifetime.Scoped, services.Single(x => x.ServiceType == typeof(AzureStorageTaskHubBrowser)).Lifetime);
    }
}
