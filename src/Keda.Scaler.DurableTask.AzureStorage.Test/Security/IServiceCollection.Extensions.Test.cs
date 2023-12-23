// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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
    private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

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
    public void GivenCertificateAuthenticationOverrides_WhenAddingTlsSupport_ThenMapOptions()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new KeyValuePair<string, string?>[]
            {
                new("Security:Transport:Client:Authentication:RevocationMode", "NoCheck"),
            })
            .Build();

        using ServiceProvider serviceProvider = new ServiceCollection()
            .AddSingleton(config)
            .AddLogging()
            .AddTlsSupport("default", config)
            .BuildServiceProvider();

        CertificateValidationOptions upstream = serviceProvider
            .GetRequiredService<IOptionsMonitor<CertificateValidationOptions>>()
            .Get(CertificateAuthenticationDefaults.AuthenticationScheme);

        Assert.Equal(X509RevocationMode.NoCheck, upstream.RevocationMode);

        CertificateAuthenticationOptions actual = serviceProvider
            .GetRequiredService<IOptionsMonitor<CertificateAuthenticationOptions>>()
            .Get(CertificateAuthenticationDefaults.AuthenticationScheme);

        Assert.Equal(X509RevocationMode.NoCheck, actual.RevocationMode);
    }

    [Fact]
    public void GivenUnsafeTls_WhenAddingTlsSupport_ThenAddServiceSubset()
    {
        IConfiguration config = new ConfigurationBuilder().Build();
        IServiceCollection services = new ServiceCollection()
            .AddSingleton(config)
            .AddLogging()
            .AddTlsSupport("default", config);

        Assert.Empty(services.Where(s => s.ServiceType == typeof(CertificateFileMonitor) && Equals(s.ServiceKey, "server")));
        Assert.Empty(services.Where(s => s.ServiceType == typeof(CertificateFileMonitor) && Equals(s.ServiceKey, "clientca")));

        Assert.Equal(2, services.Count(s => s.ServiceType == typeof(IConfigureOptions<CertificateAuthenticationOptions>)));
        _ = Assert.Single(services.Where(s => s.ServiceType == typeof(IConfigureOptions<CertificateValidationOptions>)));
        _ = Assert.Single(services.Where(s => s.ServiceType == typeof(IConfigureOptions<CertificateValidationCacheOptions>)));
        _ = Assert.Single(services.Where(s => s.ServiceType == typeof(IConfigureOptions<TlsClientOptions>)));
        _ = Assert.Single(services.Where(s => s.ServiceType == typeof(IConfigureOptions<TlsServerOptions>)));
        _ = Assert.Single(services.Where(s => s.ServiceType == typeof(IValidateOptions<TlsClientOptions>)));
        _ = Assert.Single(services.Where(s => s.ServiceType == typeof(IValidateOptions<TlsServerOptions>)));
        _ = Assert.Single(services.Where(s => s.ServiceType == typeof(TlsConfigure)));

        // No authentication or authorization services
        Assert.Empty(services.Where(s => s.ServiceType == typeof(IAuthenticationService)));
        Assert.Empty(services.Where(s => s.ServiceType == typeof(IAuthorizationService)));

        // Assert that TLSConfigure is re-used
        // Note: The services will be at the end of the collection, so the service provider should resolve them
        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        TlsConfigure expected = serviceProvider.GetRequiredService<TlsConfigure>();
        Assert.Same(expected, serviceProvider.GetRequiredService<IConfigureOptions<CertificateAuthenticationOptions>>());
        Assert.Same(expected, serviceProvider.GetRequiredService<IOptionsChangeTokenSource<CertificateAuthenticationOptions>>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GivenMutualTls_WhenAddingTlsSupport_ThenAddAllServices(bool useCustomCa)
    {
        const string CertName = "example.crt";
        string certPath = Path.Combine(_tempFolder, CertName);

        try
        {
            using RSA key = RSA.Create();
            using X509Certificate2 cert = key.CreateSelfSignedCertificate();
            _ = Directory.CreateDirectory(_tempFolder);
            File.WriteAllText(certPath, cert.ExportCertificatePem(key));

            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(new KeyValuePair<string, string?>[]
                {
                    new("Security:Transport:Client:CaCertificatePath", useCustomCa ? certPath : null),
                    new("Security:Transport:Client:ValidateCertificate", "true"),
                    new("Security:Transport:Server:CertificatePath", certPath),
                })
                .Build();

            IServiceCollection services = new ServiceCollection()
                .AddSingleton(config)
                .AddLogging()
                .AddTlsSupport("default", config);

            _ = Assert.Single(services.Where(s => s.ServiceType == typeof(CertificateFileMonitor) && Equals(s.ServiceKey, "server")));

            IEnumerable<ServiceDescriptor> clientCaDescriptors = services.Where(s => s.ServiceType == typeof(CertificateFileMonitor) && Equals(s.ServiceKey, "clientca"));
            if (useCustomCa)
                _ = Assert.Single(clientCaDescriptors);
            else
                Assert.Empty(clientCaDescriptors);

            Assert.Equal(2, services.Count(s => s.ServiceType == typeof(IConfigureOptions<CertificateAuthenticationOptions>)));
            _ = Assert.Single(services.Where(s => s.ServiceType == typeof(IConfigureOptions<CertificateValidationOptions>)));
            _ = Assert.Single(services.Where(s => s.ServiceType == typeof(IConfigureOptions<CertificateValidationCacheOptions>)));
            _ = Assert.Single(services.Where(s => s.ServiceType == typeof(IConfigureOptions<TlsClientOptions>)));
            _ = Assert.Single(services.Where(s => s.ServiceType == typeof(IConfigureOptions<TlsServerOptions>)));
            _ = Assert.Single(services.Where(s => s.ServiceType == typeof(IValidateOptions<TlsClientOptions>)));
            _ = Assert.Single(services.Where(s => s.ServiceType == typeof(IValidateOptions<TlsServerOptions>)));
            _ = Assert.Single(services.Where(s => s.ServiceType == typeof(TlsConfigure)));

            _ = Assert.Single(services.Where(s => s.ServiceType == typeof(IAuthenticationService)));
            _ = Assert.Single(services.Where(s => s.ServiceType == typeof(ICertificateValidationCache)));
            _ = Assert.Single(services.Where(s => s.ServiceType == typeof(IAuthorizationService)));
            _ = Assert.Single(services.Where(s => s.ServiceType == typeof(IConfigureOptions<AuthorizationOptions>)));

            // Assert that TLSConfigure is re-used
            // Note: The services will be at the end of the collection, so the service provider should resolve them
            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            TlsConfigure expected = serviceProvider.GetRequiredService<TlsConfigure>();
            Assert.Same(expected, serviceProvider.GetRequiredService<IConfigureOptions<CertificateAuthenticationOptions>>());
            Assert.Same(expected, serviceProvider.GetRequiredService<IOptionsChangeTokenSource<CertificateAuthenticationOptions>>());
        }
        finally
        {
            Directory.Delete(_tempFolder, recursive: true);
        }
    }
}
