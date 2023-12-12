// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Keda.Scaler.DurableTask.AzureStorage.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Security;

public class IServiceCollectionExtensionsTest
{
    [Fact]
    public void GivenNullServiceCollection_WhenAddingTlsSupport_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => IServiceCollectionExtensions.AddTlsSupport(null!, "foobar", Substitute.For<IConfiguration>()));

    [Fact]
    public void GivenNullPolicyName_WhenAddingTlsSupport_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddTlsSupport(null!, Substitute.For<IConfiguration>()));

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void GivenEmptyOrWhiteSpacePolicyName_WhenAddingTlsSupport_ThenThrowArgumentException(string policyName)
        => Assert.Throws<ArgumentException>(() => new ServiceCollection().AddTlsSupport(policyName, Substitute.For<IConfiguration>()));

    [Fact]
    public void GivenNullConfiguration_WhenAddingTlsSupport_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddTlsSupport("baz", null!));

    [Fact]
    public void GivenUnsafeTls_WhenAddingTlsSupport_ThenAddServiceSubset()
    {
        IConfiguration config = new ConfigurationBuilder().Build();
        IServiceCollection services = new ServiceCollection().AddTlsSupport("default", config);

        Assert.Equal(2, services.Count(s => s.ServiceType == typeof(IConfigureOptions<CertificateAuthenticationOptions>)));
        _ = Assert.Single(services.Where(s => s.ServiceType == typeof(IOptionsChangeTokenSource<CertificateAuthenticationOptions>)));
        _ = Assert.Single(services.Where(s => s.ServiceType == typeof(IConfigureOptions<CertificateValidationCacheOptions>)));
        _ = Assert.Single(services.Where(s => s.ServiceType == typeof(IConfigureOptions<TlsClientOptions>)));
        _ = Assert.Single(services.Where(s => s.ServiceType == typeof(IConfigureOptions<TlsServerOptions>)));
        _ = Assert.Single(services.Where(s => s.ServiceType == typeof(IValidateOptions<TlsClientOptions>)));
        _ = Assert.Single(services.Where(s => s.ServiceType == typeof(IValidateOptions<TlsServerOptions>)));
        _ = Assert.Single(services.Where(s => s.ServiceType == typeof(TlsConfigure)));

        // No authentication or authorization services
        Assert.Empty(services.Where(s => s.ServiceType == typeof(IAuthenticationService)));
        Assert.Empty(services.Where(s => s.ServiceType == typeof(IAuthorizationService)));
    }

    [Fact]
    public void GivenMutualTls_WhenAddingTlsSupport_ThenAddAllServices()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new KeyValuePair<string, string?>[]
            {
                new("Security:Transport:Client:CertificatePath", "example.crt"),
                new("Security:Transport:Server:ValidateCertificate", "true"),
            })
            .Build();

        IServiceCollection services = new ServiceCollection().AddTlsSupport("default", config);

        Assert.Equal(2, services.Count(s => s.ServiceType == typeof(IConfigureOptions<CertificateAuthenticationOptions>)));
        _ = Assert.Single(services.Where(s => s.ServiceType == typeof(IOptionsChangeTokenSource<CertificateAuthenticationOptions>)));
        _ = Assert.Single(services.Where(s => s.ServiceType == typeof(IConfigureOptions<CertificateValidationCacheOptions>)));
        _ = Assert.Single(services.Where(s => s.ServiceType == typeof(IConfigureOptions<TlsClientOptions>)));
        _ = Assert.Single(services.Where(s => s.ServiceType == typeof(IConfigureOptions<TlsServerOptions>)));
        _ = Assert.Single(services.Where(s => s.ServiceType == typeof(IValidateOptions<TlsClientOptions>)));
        _ = Assert.Single(services.Where(s => s.ServiceType == typeof(IValidateOptions<TlsServerOptions>)));
        _ = Assert.Single(services.Where(s => s.ServiceType == typeof(TlsConfigure)));

        _ = Assert.Single(services.Where(s => s.ServiceType == typeof(IAuthenticationService)));
        _ = Assert.Single(services.Where(s => s.ServiceType == typeof(IConfigureOptions<CertificateAuthenticationOptions>)));
        _ = Assert.Single(services.Where(s => s.ServiceType == typeof(ICertificateValidationCache)));
        _ = Assert.Single(services.Where(s => s.ServiceType == typeof(IAuthorizationService)));
        _ = Assert.Single(services.Where(s => s.ServiceType == typeof(IConfigureOptions<AuthorizationOptions>)));
    }
}
