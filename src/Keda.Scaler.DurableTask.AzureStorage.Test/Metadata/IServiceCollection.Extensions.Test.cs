// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Linq;
using Keda.Scaler.DurableTask.AzureStorage.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Metadata;

public class IServiceCollectionExtensionsTest
{
    private readonly ServiceCollection _servicCollection = new();

    [Fact]
    public void GivenNullServiceCollection_WhenAdddingAzureStorageServiceClients_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => IServiceCollectionExtensions.AddScalerMetadata(null!));

    [Fact]
    public void GivenServiceCollection_WhenAddingAzureStorageServiceClients_ThenRegisterServices()
    {
        IServiceCollection services = _servicCollection.AddScalerMetadata();

        ServiceDescriptor accessor = Assert.Single(services, x => x.ServiceType == typeof(IScalerMetadataAccessor));
        Assert.Equal(ServiceLifetime.Scoped, accessor.Lifetime);
        Assert.Equal(typeof(ScalerMetadataAccessor), accessor.ImplementationType);

        ServiceDescriptor configure = Assert.Single(services, x => x.ServiceType == typeof(IConfigureOptions<ScalerOptions>));
        Assert.Equal(ServiceLifetime.Scoped, configure.Lifetime);
        Assert.Equal(typeof(ScalerMetadataAccessor), configure.ImplementationType);

        ServiceDescriptor[] validators = services.Where(x => x.ServiceType == typeof(IValidateOptions<ScalerOptions>)).ToArray();
        Assert.Equal(2, validators.Length);
        Assert.Equal(ServiceLifetime.Singleton, validators[0].Lifetime);
        Assert.Equal(typeof(ValidateTaskHubScalerOptions), validators[0].ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, validators[1].Lifetime);
        Assert.Equal(typeof(ValidateScalerOptions), validators[1].ImplementationType);
    }
}
