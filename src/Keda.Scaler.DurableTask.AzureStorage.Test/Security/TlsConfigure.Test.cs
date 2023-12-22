// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using Keda.Scaler.DurableTask.AzureStorage.Security;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Xunit;
using Xunit.Abstractions;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Security;

public class TlsConfigureTest(ITestOutputHelper outputHelper) : TlsCertificateTest(outputHelper)
{
    [Fact]
    public void GivenNullTlsClientOptions_WhenCreatingTlsConfigure_ThenThrowArgumentNullException()
    {
        _ = Assert.Throws<ArgumentNullException>(() => new TlsConfigure(null!, LoggerFactory));
        _ = Assert.Throws<ArgumentNullException>(() => new TlsConfigure(Options.Create<TlsClientOptions>(null!), LoggerFactory));
        _ = Assert.Throws<ArgumentNullException>(() => new TlsConfigure(Server, null!, LoggerFactory));
        _ = Assert.Throws<ArgumentNullException>(() => new TlsConfigure(Server, Options.Create<TlsClientOptions>(null!), LoggerFactory));
        _ = Assert.Throws<ArgumentNullException>(() => new TlsConfigure(Server, ClientCa, null!, LoggerFactory));
        _ = Assert.Throws<ArgumentNullException>(() => new TlsConfigure(Server, ClientCa, Options.Create<TlsClientOptions>(null!), LoggerFactory));
    }

    [Fact]
    public void GivenNullLoggerFactory_WhenCreatingTlsConfigure_ThenThrowArgumentNullException()
    {
        _ = Assert.Throws<ArgumentNullException>(() => new TlsConfigure(Options.Create(new TlsClientOptions()), null!));
        _ = Assert.Throws<ArgumentNullException>(() => new TlsConfigure(Server, Options.Create(new TlsClientOptions()), null!));
        _ = Assert.Throws<ArgumentNullException>(() => new TlsConfigure(Server, ClientCa, Options.Create(new TlsClientOptions()), null!));
    }

    [Fact]
    public void GivenNullOptions_WhenConfiguringHttpsConnectionAdapterOptions_ThenThrowArgumentNullException()
    {
        TlsConfigure configure = new(Options.Create(new TlsClientOptions()), LoggerFactory);
        _ = Assert.Throws<ArgumentNullException>(() => configure.Configure((HttpsConnectionAdapterOptions)null!));
    }

    [Fact]
    public void GivenNoTls_WhenConfiguringHttpsConnectionAdapterOptions_ThenDoNotSupplyCertificate()
    {
        TlsConfigure configure = new(Options.Create(new TlsClientOptions()), LoggerFactory);

        HttpsConnectionAdapterOptions options = new();
        configure.Configure(options);

        Assert.Null(options.ServerCertificateSelector);
        Assert.Equal(ClientCertificateMode.NoCertificate, options.ClientCertificateMode);
    }

    [Theory]
    [InlineData(ClientCertificateMode.NoCertificate, false)]
    [InlineData(ClientCertificateMode.RequireCertificate, true)]
    public void GivenTls_WhenConfiguringHttpsConnectionAdapterOptions_ThenConfigureClientValidationAppropriately(ClientCertificateMode expected, bool validate)
    {
        TlsClientOptions clientOptions = new() { ValidateCertificate = validate };
        TlsConfigure configure = new(Server, Options.Create(clientOptions), LoggerFactory);

        HttpsConnectionAdapterOptions options = new();
        configure.Configure(options);

        Assert.NotNull(options.ServerCertificateSelector);
        Assert.Equal(expected, options.ClientCertificateMode);
    }

    [Fact]
    public void GivenNullOptions_WhenConfiguringCertificateAuthenticationOptions_ThenThrowArgumentNullException()
    {
        TlsConfigure configure = new(Options.Create(new TlsClientOptions()), LoggerFactory);
        _ = Assert.Throws<ArgumentNullException>(() => configure.Configure((CertificateAuthenticationOptions)null!));
        _ = Assert.Throws<ArgumentNullException>(() => configure.Configure("foo", null!));
    }

    [Fact]
    public void GivenInvalidName_WhenConfiguringCertificateAuthenticationOptions_ThenSkipConfiguring()
    {
        TlsConfigure configure = new(Server, ClientCa, Options.Create(new TlsClientOptions()), LoggerFactory);

        CertificateAuthenticationOptions options = new();

        configure.Configure(options);
        Assert.Equal(X509ChainTrustMode.System, options.ChainTrustValidationMode);
        Assert.Empty(options.CustomTrustStore);

        configure.Configure("other", options);
        Assert.Equal(X509ChainTrustMode.System, options.ChainTrustValidationMode);
        Assert.Empty(options.CustomTrustStore);
    }

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public void GivenUnsafeOrNoTls_WhenConfiguringCertificateAuthenticationOptions_ThenSkipCustomCertificate(bool specifyServerCert, bool validateClientCert, bool customCertAuthority)
    {
        TlsClientOptions clientOptions = new() { ValidateCertificate = validateClientCert };
        TlsConfigure configure = new(
            specifyServerCert ? Server : null!,
            customCertAuthority ? ClientCa : null!,
            Options.Create(clientOptions),
            LoggerFactory);

        CertificateAuthenticationOptions options = new();
        configure.Configure(CertificateAuthenticationDefaults.AuthenticationScheme, options);

        Assert.Equal(X509ChainTrustMode.System, options.ChainTrustValidationMode);
        Assert.Empty(options.CustomTrustStore);
    }

    [Fact]
    public void GivenExpectedNameAndCustomCa_WhenConfiguringCertificateAuthenticationOptions_ThenUpdateOptions()
    {
        TlsConfigure configure = new(Server, ClientCa, Options.Create(new TlsClientOptions()), LoggerFactory);
        CertificateAuthenticationOptions options = new();

        Assert.Equal(X509ChainTrustMode.System, options.ChainTrustValidationMode);
        configure.Configure(CertificateAuthenticationDefaults.AuthenticationScheme, options);

        Assert.Equal(X509ChainTrustMode.CustomRootTrust, options.ChainTrustValidationMode);
        X509Certificate2 actual = Assert.Single(options.CustomTrustStore);
        Assert.Equal(ClientCa.Current.Thumbprint, actual.Thumbprint);
    }

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public void GivenNoCustomCaMonitoring_WhenMonitoringChangesForOptions_ThenReturnNullToken(bool specifyServerCert, bool validateClientCert, bool customCertAuthority)
    {
        TlsClientOptions clientOptions = new() { ValidateCertificate = validateClientCert };
        TlsConfigure configure = new(
            specifyServerCert ? Server : null!,
            customCertAuthority ? ClientCa : null!,
            Options.Create(clientOptions),
            LoggerFactory);

        Assert.Equal(CertificateAuthenticationDefaults.AuthenticationScheme, ((IOptionsChangeTokenSource<CertificateAuthenticationOptions>)configure).Name);

        IChangeToken actual = configure.GetChangeToken();
        Assert.Same(NullChangeToken.Singleton, actual);
    }

    [Fact]
    public void GivenCustomCa_WhenMonitoringChangesForOptions_ThenReturnVaidToken()
    {
        TlsConfigure configure = new(Server, ClientCa, Options.Create(new TlsClientOptions()), LoggerFactory);

        Assert.Equal(CertificateAuthenticationDefaults.AuthenticationScheme, ((IOptionsChangeTokenSource<CertificateAuthenticationOptions>)configure).Name);

        IChangeToken actual = configure.GetChangeToken();
        Assert.NotSame(NullChangeToken.Singleton, actual);
    }
}
