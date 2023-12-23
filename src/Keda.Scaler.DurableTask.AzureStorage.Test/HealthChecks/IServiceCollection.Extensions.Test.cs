// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Grpc.HealthCheck;
using Keda.Scaler.DurableTask.AzureStorage.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.HealthChecks;

public class IServiceCollectionExtensionsTest
{
    [Fact]
    public void GivenNullServiceCollection_WhenAddingKubernetesHealthCheck_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => IServiceCollectionExtensions.AddKubernetesHealthCheck(null!, Substitute.For<IConfiguration>()));

    [Fact]
    public void GivenNullConfiguration_WhenAddingKubernetesHealthCheck_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddKubernetesHealthCheck(null!));

    [Fact]
    public void GivenNoTls_WhenAddingKubernetesHealthCheck_ThenSkipHealthCheck()
    {
        IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        IServiceCollection services = new ServiceCollection().AddKubernetesHealthCheck(config);

        // Options
        Assert.Equal(ServiceLifetime.Singleton, services.Single(x =>
            x.ServiceType == typeof(IValidateOptions<HealthCheckOptions>) &&
            x.ImplementationType == typeof(ValidateHealthCheckOptions)).Lifetime);
        Assert.Equal(ServiceLifetime.Transient, services.Single(x =>
            x.ServiceType == typeof(IConfigureOptions<HealthCheckOptions>)).Lifetime);

        // Skipped
        Assert.Empty(services.Where(x => x.ServiceType == typeof(HealthServiceImpl)));
    }

    [Fact]
    public void GivenServiceCollection_WhenAddingKubernetesHealthCheck_ThenAddExpectedServices()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new KeyValuePair<string, string?>[]
            {
                new("Security:Transport:Server:CertificatePath", "tls.crt"),
            })
            .Build();

        IServiceCollection services = new ServiceCollection().AddKubernetesHealthCheck(config);

        // Singleton
        Assert.Equal(ServiceLifetime.Singleton, services.Single(x =>
            x.ServiceType == typeof(IValidateOptions<HealthCheckOptions>) &&
            x.ImplementationType == typeof(ValidateHealthCheckOptions)).Lifetime);
        Assert.Equal(ServiceLifetime.Singleton, services.Single(x =>
            x.ServiceType == typeof(HealthServiceImpl) &&
            x.ImplementationType == typeof(HealthServiceImpl)).Lifetime);

        // Transient
        Assert.Equal(ServiceLifetime.Transient, services.Single(x =>
            x.ServiceType == typeof(IConfigureOptions<HealthCheckOptions>)).Lifetime);

        // Build to check the registration
        HealthCheckServiceOptions options = services
            .BuildServiceProvider()
            .GetRequiredService<IOptions<HealthCheckServiceOptions>>()
            .Value;

        HealthCheckRegistration actual = Assert.Single(options.Registrations);
        Assert.Equal("TLS", actual.Name);
    }
}
