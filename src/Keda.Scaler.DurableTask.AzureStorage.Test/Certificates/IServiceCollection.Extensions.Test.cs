// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Linq;
using Keda.Scaler.DurableTask.AzureStorage.Certificates;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Certificates;

public class IServiceCollectionExtensionsTest
{
    [Fact]
    public void GivenNullServiceCollection_WhenAddingMutualTlsSupport_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => IServiceCollectionExtensions.AddMutualTlsSupport(null!, "foobar", Substitute.For<IConfiguration>()));

    [Fact]
    public void GivenNullPolicyName_WhenAddingMutualTlsSupport_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddMutualTlsSupport(null!, Substitute.For<IConfiguration>()));

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void GivenEmptyOrWhiteSpacePolicyName_WhenAddingMutualTlsSupport_ThenThrowArgumentException(string policyName)
        => Assert.Throws<ArgumentException>(() => new ServiceCollection().AddMutualTlsSupport(policyName, Substitute.For<IConfiguration>()));

    [Fact]
    public void GivenNullConfiguration_WhenAddingMutualTlsSupport_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddMutualTlsSupport("baz", null!));

    [Fact]
    public void GivenNoCertificationValidation_WhenAddingMutualTlsSupport_ThenSkipAuthenticationAndAuthorizationServices()
    {
        IConfiguration config = new ConfigurationBuilder().Build();
        IServiceCollection services = new ServiceCollection().AddMutualTlsSupport("default", config);

        ServiceDescriptor actual = Assert.Single(services, s => s.ServiceType == typeof(IValidateOptions<ClientCertificateValidationOptions>));
        Assert.Equal(typeof(ValidateClientCertificateValidationOptions), actual.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);

        _ = Assert.Single(services, s => s.ServiceType == typeof(IConfigureOptions<ClientCertificateValidationOptions>));
        _ = Assert.Single(services, s => s.ServiceType == typeof(IConfigureOptions<CertificateAuthenticationOptions>));
        Assert.DoesNotContain(services, s => s.ServiceType == typeof(IAuthenticationService));
        Assert.DoesNotContain(services, s => s.ServiceType == typeof(IAuthorizationService));
        Assert.DoesNotContain(services, s => s.ServiceType == typeof(ConfigureCustomTrustStore));
    }

    [Fact]
    public void GivenSystemClientCertificateAuthorities_WhenAddingMutualTlsSupport_ThenSkipCustomTrustStore()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new("Kestrel:Certificates:Default:Path", "/example/cert.pem"),
                new("Kestrel:Client:Certificate:Validation:CertificateAuthority:Path", null),
                new("Kestrel:Client:Certificate:Validation:Enabled", "true"),
            ])
            .Build();

        IServiceCollection services = new ServiceCollection().AddMutualTlsSupport("default", config);

        ServiceDescriptor actual = Assert.Single(services, s => s.ServiceType == typeof(IValidateOptions<ClientCertificateValidationOptions>));
        Assert.Equal(typeof(ValidateClientCertificateValidationOptions), actual.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);

        _ = Assert.Single(services, s => s.ServiceType == typeof(IConfigureOptions<ClientCertificateValidationOptions>));
        _ = Assert.Single(services, s => s.ServiceType == typeof(IConfigureOptions<CertificateAuthenticationOptions>));
        _ = Assert.Single(services, s => s.ServiceType == typeof(IAuthenticationService));
        _ = Assert.Single(services, s => s.ServiceType == typeof(IAuthorizationService));
        _ = Assert.Single(services, s => s.ServiceType == typeof(ICertificateValidationCache));
        Assert.DoesNotContain(services, s => s.ServiceType == typeof(ConfigureCustomTrustStore));
    }

    [Fact]
    public void GivenCustomTrustStore_WhenAddingMutualTlsSupport_ThenSkipCustomTrustStore()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new("Kestrel:Certificates:Default:Path", "/example/cert.pem"),
                new("Kestrel:Client:Certificate:Validation:CertificateAuthority:Path", "/example/ca.crt"),
                new("Kestrel:Client:Certificate:Validation:Enabled", "true"),
            ])
            .Build();

        IServiceCollection services = new ServiceCollection().AddMutualTlsSupport("default", config);

        ServiceDescriptor actual;

        actual = Assert.Single(services, s => s.ServiceType == typeof(IValidateOptions<ClientCertificateValidationOptions>));
        Assert.Equal(typeof(ValidateClientCertificateValidationOptions), actual.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);

        _ = Assert.Single(services, s => s.ServiceType == typeof(IConfigureOptions<ClientCertificateValidationOptions>));
        Assert.Equal(2, services.Count(s => s.ServiceType == typeof(IConfigureOptions<CertificateAuthenticationOptions>)));
        _ = Assert.Single(services, s => s.ServiceType == typeof(IOptionsChangeTokenSource<CertificateAuthenticationOptions>));
        _ = Assert.Single(services, s => s.ServiceType == typeof(IAuthenticationService));
        _ = Assert.Single(services, s => s.ServiceType == typeof(IAuthorizationService));
        _ = Assert.Single(services, s => s.ServiceType == typeof(ICertificateValidationCache));

        actual = Assert.Single(services, s => s.ServiceType == typeof(ConfigureCustomTrustStore));
        Assert.Equal(typeof(ConfigureCustomTrustStore), actual.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);
    }
}
