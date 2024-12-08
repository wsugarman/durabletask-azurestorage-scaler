// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Linq;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Keda.Scaler.DurableTask.AzureStorage.Accounts;
using Keda.Scaler.DurableTask.AzureStorage.Clients;
using Keda.Scaler.DurableTask.AzureStorage.TaskHubs;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Web;

public class IServiceCollectionExtensionsTest
{
    [Fact]
    public void GivenNullServiceCollection_WhenAddingDurableTaskScalerServices_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => IServiceCollectionExtensions.AddDurableTaskScaler(null!));

    [Fact]
    public void GivenServiceCollection_WhenAddingDurableTaskScalerServices_ThenAddExpectedServices()
    {
        IServiceCollection services = new ServiceCollection().AddDurableTaskScaler();
        Assert.Equal(4, services.Count);

        // Singleton
        Assert.Equal(ServiceLifetime.Singleton, services.Single(x =>
            x.ServiceType == typeof(IStorageAccountClientFactory<BlobServiceClient>) &&
            x.ImplementationType == typeof(BlobServiceClientFactory)).Lifetime);
        Assert.Equal(ServiceLifetime.Singleton, services.Single(x =>
            x.ServiceType == typeof(IStorageAccountClientFactory<QueueServiceClient>) &&
            x.ImplementationType == typeof(QueueServiceClientFactory)).Lifetime);
        Assert.Equal(ServiceLifetime.Singleton, services.Single(x =>
            x.ServiceType == typeof(DurableTaskScaleManager) &&
            x.ImplementationType == typeof(OptimalDurableTaskScaleManager)).Lifetime);

        // Scoped
        Assert.Equal(ServiceLifetime.Scoped, services.Single(x => x.ServiceType == typeof(AzureStorageTaskHubClient)).Lifetime);
    }
}
