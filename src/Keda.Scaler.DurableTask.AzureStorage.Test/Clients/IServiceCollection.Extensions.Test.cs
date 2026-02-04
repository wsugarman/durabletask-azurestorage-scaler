// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Keda.Scaler.DurableTask.AzureStorage.Clients;
using Keda.Scaler.DurableTask.AzureStorage.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Clients;

[TestClass]
public class IServiceCollectionExtensionsTest
{
    private readonly ServiceCollection _servicCollection = new();

    [TestMethod]
    public void GivenNullServiceCollection_WhenAdddingAzureStorageServiceClients_ThenThrowArgumentNullException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => AzureStorage.Clients.IServiceCollectionExtensions.AddAzureStorageServiceClients(null!));

    [TestMethod]
    public void GivenServiceCollection_WhenAddingAzureStorageServiceClients_ThenRegisterServices()
    {
        IServiceCollection services = _servicCollection.AddAzureStorageServiceClients();

        ServiceDescriptor blobFactory = Assert.ContainsSingle(x => x.ServiceType == typeof(BlobServiceClientFactory), services);
        Assert.AreEqual(ServiceLifetime.Singleton, blobFactory.Lifetime);
        Assert.AreEqual(typeof(BlobServiceClientFactory), blobFactory.ImplementationType);

        ServiceDescriptor queueFatory = Assert.ContainsSingle(x => x.ServiceType == typeof(QueueServiceClientFactory), services);
        Assert.AreEqual(ServiceLifetime.Singleton, queueFatory.Lifetime);
        Assert.AreEqual(typeof(QueueServiceClientFactory), queueFatory.ImplementationType);

        ServiceDescriptor tableFactory = Assert.ContainsSingle(x => x.ServiceType == typeof(TableServiceClientFactory), services);
        Assert.AreEqual(ServiceLifetime.Singleton, tableFactory.Lifetime);
        Assert.AreEqual(typeof(TableServiceClientFactory), tableFactory.ImplementationType);

        ServiceDescriptor configure = Assert.ContainsSingle(x => x.ServiceType == typeof(IConfigureOptions<AzureStorageAccountOptions>), services);
        Assert.AreEqual(ServiceLifetime.Scoped, configure.Lifetime);
        Assert.AreEqual(typeof(ConfigureAzureStorageAccountOptions), configure.ImplementationType);

        ServiceDescriptor validate = Assert.ContainsSingle(x => x.ServiceType == typeof(IValidateOptions<AzureStorageAccountOptions>), services);
        Assert.AreEqual(ServiceLifetime.Scoped, validate.Lifetime);
        Assert.AreEqual(typeof(ValidateAzureStorageAccountOptions), validate.ImplementationType);

        ServiceDescriptor blobClient = Assert.ContainsSingle(x => x.ServiceType == typeof(BlobServiceClient), services);
        Assert.AreEqual(ServiceLifetime.Scoped, blobClient.Lifetime);

        ServiceDescriptor queueClient = Assert.ContainsSingle(x => x.ServiceType == typeof(QueueServiceClient), services);
        Assert.AreEqual(ServiceLifetime.Scoped, queueClient.Lifetime);

        ServiceDescriptor tableClient = Assert.ContainsSingle(x => x.ServiceType == typeof(TableServiceClient), services);
        Assert.AreEqual(ServiceLifetime.Scoped, tableClient.Lifetime);
    }

    [TestMethod]
    public void GivenAzureStorageAccountOptions_WhenBlobServiceClient_ThenUseFactoryToCreateClient()
    {
        IServiceCollection services = _servicCollection
            .Configure<ScalerOptions>(o => o.Connection = "UseDevelopmentStorage=true")
            .AddAzureStorageServiceClients();

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        Assert.IsNotNull(scope.ServiceProvider.GetService<BlobServiceClient>());
    }

    [TestMethod]
    public void GivenAzureStorageAccountOptions_WhenQueueServiceClient_ThenUseFactoryToCreateClient()
    {
        IServiceCollection services = _servicCollection
            .Configure<ScalerOptions>(o => o.Connection = "UseDevelopmentStorage=true")
            .AddAzureStorageServiceClients();

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        Assert.IsNotNull(scope.ServiceProvider.GetService<QueueServiceClient>());
    }

    [TestMethod]
    public void GivenAzureStorageAccountOptions_WhenTableServiceClient_ThenUseFactoryToCreateClient()
    {
        IServiceCollection services = _servicCollection
            .Configure<ScalerOptions>(o => o.Connection = "UseDevelopmentStorage=true")
            .AddAzureStorageServiceClients();

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        Assert.IsNotNull(scope.ServiceProvider.GetService<TableServiceClient>());
    }
}
