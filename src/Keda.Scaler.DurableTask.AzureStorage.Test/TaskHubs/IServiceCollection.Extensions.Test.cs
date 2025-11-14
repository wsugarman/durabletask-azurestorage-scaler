// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Keda.Scaler.DurableTask.AzureStorage.Metadata;
using Keda.Scaler.DurableTask.AzureStorage.TaskHubs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHubs;

[TestClass]
public class IServiceCollectionExtensionsTest
{
    private readonly ServiceCollection _servicCollection = new();

    [TestMethod]
    public void GivenNullServiceCollection_WhenAddingDurableTaskScaleManager_ThenThrowArgumentNullException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => AzureStorage.TaskHubs.IServiceCollectionExtensions.AddDurableTaskScaleManager(null!));

    [TestMethod]
    public void GivenServiceCollection_WhenAddingDurableTaskScalerManager_ThenRegisterServices()
    {
        IServiceCollection services = _servicCollection.AddDurableTaskScaleManager();

        ServiceDescriptor configure = Assert.ContainsSingle(x => x.ServiceType == typeof(IConfigureOptions<TaskHubOptions>), services);
        Assert.AreEqual(ServiceLifetime.Scoped, configure.Lifetime);
        Assert.AreEqual(typeof(ConfigureTaskHubOptions), configure.ImplementationType);

        ServiceDescriptor blobPartitionManager = Assert.ContainsSingle(x => x.ServiceType == typeof(BlobPartitionManager), services);
        Assert.AreEqual(ServiceLifetime.Scoped, blobPartitionManager.Lifetime);
        Assert.AreEqual(typeof(BlobPartitionManager), blobPartitionManager.ImplementationType);

        ServiceDescriptor tablePartitionManager = Assert.ContainsSingle(x => x.ServiceType == typeof(TablePartitionManager), services);
        Assert.AreEqual(ServiceLifetime.Scoped, tablePartitionManager.Lifetime);
        Assert.AreEqual(typeof(TablePartitionManager), tablePartitionManager.ImplementationType);

        ServiceDescriptor partitionManager = Assert.ContainsSingle(x => x.ServiceType == typeof(ITaskHubPartitionManager), services);
        Assert.AreEqual(ServiceLifetime.Scoped, partitionManager.Lifetime);

        ServiceDescriptor taskHub = Assert.ContainsSingle(x => x.ServiceType == typeof(ITaskHub), services);
        Assert.AreEqual(ServiceLifetime.Scoped, taskHub.Lifetime);
        Assert.AreEqual(typeof(TaskHub), taskHub.ImplementationType);

        ServiceDescriptor scaleManager = Assert.ContainsSingle(x => x.ServiceType == typeof(DurableTaskScaleManager), services);
        Assert.AreEqual(ServiceLifetime.Scoped, scaleManager.Lifetime);
        Assert.AreEqual(typeof(OptimalDurableTaskScaleManager), scaleManager.ImplementationType);
    }

    [TestMethod]
    public void GivenTablePartitionManagement_WhenResolvingPartitionManager_ThenResolveTablePartitionManager()
    {
        IServiceCollection services = _servicCollection
            .Configure<ScalerOptions>(x => x.UseTablePartitionManagement = true)
            .AddScoped(sp => Substitute.For<TableServiceClient>())
            .AddLogging()
            .AddDurableTaskScaleManager();

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        _ = Assert.IsInstanceOfType<TablePartitionManager>(scope.ServiceProvider.GetRequiredService<ITaskHubPartitionManager>());
    }

    [TestMethod]
    public void GivenBlobPartitionManagement_WhenResolvingPartitionManager_ThenResolveBlobPartitionManager()
    {
        IServiceCollection services = _servicCollection
            .Configure<ScalerOptions>(x => x.UseTablePartitionManagement = false)
            .AddScoped(sp => Substitute.For<BlobServiceClient>())
            .AddLogging()
            .AddDurableTaskScaleManager();

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        _ = Assert.IsInstanceOfType<BlobPartitionManager>(scope.ServiceProvider.GetRequiredService<ITaskHubPartitionManager>());
    }
}
