// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Keda.Scaler.DurableTask.AzureStorage.TaskHubs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHubs;

public class IServiceCollectionExtensionsTest
{
    private readonly ServiceCollection _servicCollection = new();

    [Fact]
    public void GivenNullServiceCollection_WhenAddingDurableTaskScaleManager_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => IServiceCollectionExtensions.AddDurableTaskScaleManager(null!));

    [Fact]
    public void GivenServiceCollection_WhenAddingDurableTaskScalerManager_ThenRegisterServices()
    {
        IServiceCollection services = _servicCollection.AddDurableTaskScaleManager();

        ServiceDescriptor configure = Assert.Single(services, x => x.ServiceType == typeof(IConfigureOptions<TaskHubOptions>));
        Assert.Equal(ServiceLifetime.Singleton, configure.Lifetime);
        Assert.Equal(typeof(ConfigureTaskHubOptions), configure.ImplementationType);

        ServiceDescriptor blobPartitionManager = Assert.Single(services, x => x.ServiceType == typeof(BlobPartitionManager));
        Assert.Equal(ServiceLifetime.Scoped, blobPartitionManager.Lifetime);
        Assert.Equal(typeof(BlobPartitionManager), blobPartitionManager.ImplementationType);

        ServiceDescriptor tablePartitionManager = Assert.Single(services, x => x.ServiceType == typeof(TablePartitionManager));
        Assert.Equal(ServiceLifetime.Scoped, tablePartitionManager.Lifetime);
        Assert.Equal(typeof(TablePartitionManager), tablePartitionManager.ImplementationType);

        ServiceDescriptor partitionManager = Assert.Single(services, x => x.ServiceType == typeof(ITaskHubPartitionManager));
        Assert.Equal(ServiceLifetime.Scoped, partitionManager.Lifetime);

        ServiceDescriptor taskHub = Assert.Single(services, x => x.ServiceType == typeof(ITaskHub));
        Assert.Equal(ServiceLifetime.Scoped, taskHub.Lifetime);
        Assert.Equal(typeof(TaskHub), taskHub.ImplementationType);

        ServiceDescriptor scaleManager = Assert.Single(services, x => x.ServiceType == typeof(DurableTaskScaleManager));
        Assert.Equal(ServiceLifetime.Scoped, scaleManager.Lifetime);
        Assert.Equal(typeof(OptimalDurableTaskScaleManager), scaleManager.ImplementationType);
    }

    [Fact]
    public void GivenTablePartitionManagement_WhenResolvingPartitionManager_ThenResolveTablePartitionManager()
    {
        IServiceCollection services = _servicCollection
            .Configure<TaskHubOptions>(x => x.UseTablePartitionManagement = true)
            .AddScoped(sp => Substitute.For<TableServiceClient>())
            .AddDurableTaskScaleManager();

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        _ = Assert.IsType<TablePartitionManager>(scope.ServiceProvider.GetRequiredService<ITaskHubPartitionManager>());
    }

    [Fact]
    public void GivenBlobPartitionManagement_WhenResolvingPartitionManager_ThenResolveBlobPartitionManager()
    {
        IServiceCollection services = _servicCollection
            .Configure<TaskHubOptions>(x => x.UseTablePartitionManagement = false)
            .AddScoped(sp => Substitute.For<BlobServiceClient>())
            .AddDurableTaskScaleManager();

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        _ = Assert.IsType<BlobPartitionManager>(scope.ServiceProvider.GetRequiredService<ITaskHubPartitionManager>());
    }
}
