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
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Clients;

public class IServiceCollectionExtensionsTest
{
    private readonly ServiceCollection _servicCollection = new();

    [Fact]
    public void GivenNullServiceCollection_WhenAdddingAzureStorageServiceClients_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => AzureStorage.Clients.IServiceCollectionExtensions.AddAzureStorageServiceClients(null!));

    [Fact]
    public void GivenServiceCollection_WhenAddingAzureStorageServiceClients_ThenRegisterServices()
    {
        IServiceCollection services = _servicCollection.AddAzureStorageServiceClients();

        ServiceDescriptor blobFactory = Assert.Single(services, x => x.ServiceType == typeof(BlobServiceClientFactory));
        Assert.Equal(ServiceLifetime.Singleton, blobFactory.Lifetime);
        Assert.Equal(typeof(BlobServiceClientFactory), blobFactory.ImplementationType);

        ServiceDescriptor queueFatory = Assert.Single(services, x => x.ServiceType == typeof(QueueServiceClientFactory));
        Assert.Equal(ServiceLifetime.Singleton, queueFatory.Lifetime);
        Assert.Equal(typeof(QueueServiceClientFactory), queueFatory.ImplementationType);

        ServiceDescriptor tableFactory = Assert.Single(services, x => x.ServiceType == typeof(TableServiceClientFactory));
        Assert.Equal(ServiceLifetime.Singleton, tableFactory.Lifetime);
        Assert.Equal(typeof(TableServiceClientFactory), tableFactory.ImplementationType);

        ServiceDescriptor configure = Assert.Single(services, x => x.ServiceType == typeof(IConfigureOptions<AzureStorageAccountOptions>));
        Assert.Equal(ServiceLifetime.Scoped, configure.Lifetime);
        Assert.Equal(typeof(ConfigureAzureStorageAccountOptions), configure.ImplementationType);

        ServiceDescriptor validate = Assert.Single(services, x => x.ServiceType == typeof(IValidateOptions<AzureStorageAccountOptions>));
        Assert.Equal(ServiceLifetime.Singleton, validate.Lifetime);
        Assert.Equal(typeof(ValidateAzureStorageAccountOptions), validate.ImplementationType);

        ServiceDescriptor blobClient = Assert.Single(services, x => x.ServiceType == typeof(BlobServiceClient));
        Assert.Equal(ServiceLifetime.Scoped, blobClient.Lifetime);

        ServiceDescriptor queueClient = Assert.Single(services, x => x.ServiceType == typeof(QueueServiceClient));
        Assert.Equal(ServiceLifetime.Scoped, queueClient.Lifetime);

        ServiceDescriptor tableClient = Assert.Single(services, x => x.ServiceType == typeof(TableServiceClient));
        Assert.Equal(ServiceLifetime.Scoped, tableClient.Lifetime);
    }

    [Fact]
    public void GivenAzureStorageAccountOptions_WhenBlobServiceClient_ThenUseFactoryToCreateClient()
    {
        IServiceCollection services = _servicCollection
            .Configure<ScalerOptions>(o => o.Connection = "UseDevelopmentStorage=true")
            .AddAzureStorageServiceClients();

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetService<BlobServiceClient>());
    }

    [Fact]
    public void GivenAzureStorageAccountOptions_WhenQueueServiceClient_ThenUseFactoryToCreateClient()
    {
        IServiceCollection services = _servicCollection
            .Configure<ScalerOptions>(o => o.Connection = "UseDevelopmentStorage=true")
            .AddAzureStorageServiceClients();

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetService<QueueServiceClient>());
    }

    [Fact]
    public void GivenAzureStorageAccountOptions_WhenTableServiceClient_ThenUseFactoryToCreateClient()
    {
        IServiceCollection services = _servicCollection
            .Configure<ScalerOptions>(o => o.Connection = "UseDevelopmentStorage=true")
            .AddAzureStorageServiceClients();

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetService<TableServiceClient>());
    }
}
