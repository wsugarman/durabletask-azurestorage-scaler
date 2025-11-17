// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Linq;
using Keda.Scaler.DurableTask.AzureStorage.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Metadata;

[TestClass]
public class IServiceCollectionExtensionsTest
{
    private readonly ServiceCollection _servicCollection = new();

    [TestMethod]
    public void GivenNullServiceCollection_WhenAdddingAzureStorageServiceClients_ThenThrowArgumentNullException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => IServiceCollectionExtensions.AddScalerMetadata(null!));

    [TestMethod]
    public void GivenServiceCollection_WhenAddingAzureStorageServiceClients_ThenRegisterServices()
    {
        IServiceCollection services = _servicCollection.AddScalerMetadata();

        ServiceDescriptor accessor = Assert.ContainsSingle(x => x.ServiceType == typeof(IScalerMetadataAccessor), services);
        Assert.AreEqual(ServiceLifetime.Singleton, accessor.Lifetime);
        Assert.AreEqual(typeof(ScalerMetadataAccessor), accessor.ImplementationType);

        ServiceDescriptor configure = Assert.ContainsSingle(x => x.ServiceType == typeof(IConfigureOptions<ScalerOptions>), services);
        Assert.AreEqual(ServiceLifetime.Scoped, configure.Lifetime);
        Assert.AreEqual(typeof(ConfigureScalerOptions), configure.ImplementationType);

        ServiceDescriptor[] validators = services.Where(x => x.ServiceType == typeof(IValidateOptions<ScalerOptions>)).ToArray();
        Assert.HasCount(2, validators);
        Assert.AreEqual(ServiceLifetime.Singleton, validators[0].Lifetime);
        Assert.AreEqual(typeof(ValidateTaskHubScalerOptions), validators[0].ImplementationType);
        Assert.AreEqual(ServiceLifetime.Singleton, validators[1].Lifetime);
        Assert.AreEqual(typeof(ValidateScalerOptions), validators[1].ImplementationType);
    }
}
