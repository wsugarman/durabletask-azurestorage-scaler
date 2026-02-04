// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Keda.Scaler.DurableTask.AzureStorage.Certificates;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Certificates;

[TestClass]
public class IServiceCollectionExtensionsTest
{
    [TestMethod]
    public void GivenNullServiceCollection_WhenAddingMutualTlsSupport_ThenThrowArgumentNullException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => IServiceCollectionExtensions.AddMutualTlsSupport(null!, "foobar", Substitute.For<IConfiguration>()));

    [TestMethod]
    public void GivenNullPolicyName_WhenAddingMutualTlsSupport_ThenThrowArgumentNullException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => new ServiceCollection().AddMutualTlsSupport(null!, Substitute.For<IConfiguration>()));

    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    public void GivenEmptyOrWhiteSpacePolicyName_WhenAddingMutualTlsSupport_ThenThrowArgumentException(string policyName)
        => Assert.ThrowsExactly<ArgumentException>(() => new ServiceCollection().AddMutualTlsSupport(policyName, Substitute.For<IConfiguration>()));

    [TestMethod]
    public void GivenNullConfiguration_WhenAddingMutualTlsSupport_ThenThrowArgumentNullException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => new ServiceCollection().AddMutualTlsSupport("baz", null!));

    [TestMethod]
    public void GivenNoCertificationValidation_WhenAddingMutualTlsSupport_ThenSkipAuthenticationAndAuthorizationServices()
    {
        IConfiguration config = new ConfigurationBuilder().Build();
        IServiceCollection services = new ServiceCollection().AddMutualTlsSupport("default", config);

        ServiceDescriptor actual = Assert.ContainsSingle(s => s.ServiceType == typeof(IValidateOptions<ClientCertificateValidationOptions>), services);
        Assert.AreEqual(typeof(ValidateClientCertificateValidationOptions), actual.ImplementationType);
        Assert.AreEqual(ServiceLifetime.Singleton, actual.Lifetime);

        _ = Assert.ContainsSingle(s => s.ServiceType == typeof(IConfigureOptions<ClientCertificateValidationOptions>), services);
        _ = Assert.ContainsSingle(s => s.ServiceType == typeof(IConfigureOptions<CertificateAuthenticationOptions>), services);
        Assert.DoesNotContain(s => s.ServiceType == typeof(IAuthenticationService), services);
        Assert.DoesNotContain(s => s.ServiceType == typeof(IAuthorizationService), services);
        Assert.DoesNotContain(s => s.ServiceType == typeof(ConfigureCustomTrustStore), services);
    }

    [TestMethod]
    public void GivenSystemClientCertificateAuthorities_WhenAddingMutualTlsSupport_ThenSkipCustomTrustStore()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new("Kestrel:Certificates:Default:Path", "/example/cert.pem"),
                new($"{ClientCertificateValidationOptions.DefaultKey}:{nameof(ClientCertificateValidationOptions.Enable)}", "true"),
            ])
            .Build();

        IServiceCollection services = new ServiceCollection().AddMutualTlsSupport("default", config);

        ServiceDescriptor actual = Assert.ContainsSingle(s => s.ServiceType == typeof(IValidateOptions<ClientCertificateValidationOptions>), services);
        Assert.AreEqual(typeof(ValidateClientCertificateValidationOptions), actual.ImplementationType);
        Assert.AreEqual(ServiceLifetime.Singleton, actual.Lifetime);

        _ = Assert.ContainsSingle(s => s.ServiceType == typeof(IConfigureOptions<ClientCertificateValidationOptions>), services);
        _ = Assert.ContainsSingle(s => s.ServiceType == typeof(IConfigureOptions<CertificateAuthenticationOptions>), services);
        _ = Assert.ContainsSingle(s => s.ServiceType == typeof(IAuthenticationService), services);
        _ = Assert.ContainsSingle(s => s.ServiceType == typeof(IAuthorizationService), services);
        _ = Assert.ContainsSingle(s => s.ServiceType == typeof(ICertificateValidationCache), services);
        Assert.DoesNotContain(s => s.ServiceType == typeof(ConfigureCustomTrustStore), services);
    }

    [TestMethod]
    public void GivenCustomTrustStore_WhenAddingMutualTlsSupport_ThenSkipCustomTrustStore()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new("Kestrel:Certificates:Default:Path", "/example/cert.pem"),
                new($"{ClientCertificateValidationOptions.DefaultKey}:{nameof(ClientCertificateValidationOptions.CertificateAuthority)}:{nameof(CaCertificateFileOptions.Path)}", "/example/ca.crt"),
                new($"{ClientCertificateValidationOptions.DefaultKey}:{nameof(ClientCertificateValidationOptions.Enable)}", "true"),
            ])
            .Build();

        IServiceCollection services = new ServiceCollection().AddMutualTlsSupport("default", config);

        ServiceDescriptor actual;

        actual = Assert.ContainsSingle(s => s.ServiceType == typeof(IValidateOptions<ClientCertificateValidationOptions>), services);
        Assert.AreEqual(typeof(ValidateClientCertificateValidationOptions), actual.ImplementationType);
        Assert.AreEqual(ServiceLifetime.Singleton, actual.Lifetime);

        _ = Assert.ContainsSingle(s => s.ServiceType == typeof(IConfigureOptions<ClientCertificateValidationOptions>), services);
        Assert.AreEqual(2, services.Count(s => s.ServiceType == typeof(IConfigureOptions<CertificateAuthenticationOptions>)));
        _ = Assert.ContainsSingle(s => s.ServiceType == typeof(IOptionsChangeTokenSource<CertificateAuthenticationOptions>), services);
        _ = Assert.ContainsSingle(s => s.ServiceType == typeof(IAuthenticationService), services);
        _ = Assert.ContainsSingle(s => s.ServiceType == typeof(IAuthorizationService), services);
        _ = Assert.ContainsSingle(s => s.ServiceType == typeof(ICertificateValidationCache), services);

        actual = Assert.ContainsSingle(s => s.ServiceType == typeof(ConfigureCustomTrustStore), services);
        Assert.AreEqual(typeof(ConfigureCustomTrustStore), actual.ImplementationType);
        Assert.AreEqual(ServiceLifetime.Singleton, actual.Lifetime);
    }

    [TestMethod]
    public void GivenUserSpecifiedRevocationMode_WhenAddingMutualTlsSupport_ThenSetRevocationMode()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection([new($"{ClientCertificateValidationOptions.DefaultKey}:{nameof(ClientCertificateValidationOptions.RevocationMode)}", nameof(X509RevocationMode.NoCheck))])
            .Build();

        IServiceProvider serviceProvider = new ServiceCollection()
            .AddSingleton(config)
            .AddMutualTlsSupport("default", config)
            .BuildServiceProvider();

        IOptionsSnapshot<CertificateAuthenticationOptions> options = serviceProvider.GetRequiredService<IOptionsSnapshot<CertificateAuthenticationOptions>>();
        Assert.AreEqual(X509RevocationMode.NoCheck, options.Get(CertificateAuthenticationDefaults.AuthenticationScheme).RevocationMode);
    }
}
