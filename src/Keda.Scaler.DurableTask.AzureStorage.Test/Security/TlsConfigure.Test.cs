// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Keda.Scaler.DurableTask.AzureStorage.Security;
using Keda.Scaler.DurableTask.AzureStorage.Test.Logging;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Security;

public sealed class TlsConfigureTest : IDisposable
{
    private const string CaCertName = "ca.crt";
    private const string ServerCertName = "server.pem";
    private const string ServerKeyName = "server.key";

    private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly string _caCertPath;
    private readonly string _serverCertPath;
    private readonly string _serverKeyPath;
    private readonly RSA _caCertKey = RSA.Create();
    private readonly RSA _serverKey = RSA.Create();
    private readonly X509Certificate2 _caCertificate;
    private readonly X509Certificate2 _serverCertificate;
    private readonly ILoggerFactory _loggerFactory;

    public TlsConfigureTest(ITestOutputHelper outputHelper)
    {
        _ = Directory.CreateDirectory(_tempFolder);

        _caCertPath = Path.Combine(_tempFolder, CaCertName);
        _serverCertPath = Path.Combine(_tempFolder, ServerCertName);
        _serverKeyPath = Path.Combine(_tempFolder, ServerKeyName);
        _caCertificate = _caCertKey.CreateSelfSignedCertificate();
        _serverCertificate = _serverKey.CreateCertificate(_caCertificate, nameof(TlsConfigureTest));

        _caCertificate.WriteFile(_caCertPath);
        _serverCertificate.WriteFile(_serverCertPath);
        _serverKey.WriteFile(_serverKeyPath);

        _loggerFactory = XUnitLogger.CreateFactory(outputHelper);
    }

    public void Dispose()
    {
        _caCertKey.Dispose();
        _serverKey.Dispose();
        _caCertificate.Dispose();
        _serverCertificate.Dispose();
        _loggerFactory.Dispose();
        Directory.Delete(_tempFolder, true);
    }

    [Fact]
    public void GivenNullTlsClientOptions_WhenCreatingTlsConfigure_ThenThrowArgumentNullException()
    {
        _ = Assert.Throws<ArgumentNullException>(() => new TlsConfigure(null!, Substitute.For<IOptions<TlsServerOptions>>(), _loggerFactory));
        _ = Assert.Throws<ArgumentNullException>(() => new TlsConfigure(Options.Create<TlsClientOptions>(null!), Substitute.For<IOptions<TlsServerOptions>>(), _loggerFactory));
    }

    [Fact]
    public void GivenNullTlsServerOptions_WhenCreatingTlsConfigure_ThenThrowArgumentNullException()
    {
        _ = Assert.Throws<ArgumentNullException>(() => new TlsConfigure(Substitute.For<IOptions<TlsClientOptions>>(), null!, _loggerFactory));
        _ = Assert.Throws<ArgumentNullException>(() => new TlsConfigure(Substitute.For<IOptions<TlsClientOptions>>(), Options.Create<TlsServerOptions>(null!), _loggerFactory));
    }

    [Fact]
    public void GivenNullLoggerFactory_WhenCreatingTlsConfigure_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new TlsConfigure(Substitute.For<IOptions<TlsClientOptions>>(), Substitute.For<IOptions<TlsServerOptions>>(), null!));

    [Fact]
    public void GivenNullOptions_WhenConfiguringHttpsConnectionAdapterOptions_ThenThrowArgumentNullException()
    {
        using TlsConfigure configure = new(Options.Create(new TlsClientOptions()), Options.Create(new TlsServerOptions()), _loggerFactory);
        _ = Assert.Throws<ArgumentNullException>(() => configure.Configure((HttpsConnectionAdapterOptions)null!));
    }

    [Fact]
    public void GivenNoTls_WhenConfiguringHttpsConnectionAdapterOptions_ThenDoNotSupplyCertificate()
    {
        using TlsConfigure configure = new(Options.Create(new TlsClientOptions()), Options.Create(new TlsServerOptions()), _loggerFactory);

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
        TlsServerOptions serverOptions = new() { CertificatePath = _serverCertPath, KeyPath = _serverKeyPath };
        using TlsConfigure configure = new(Options.Create(clientOptions), Options.Create(serverOptions), _loggerFactory);

        HttpsConnectionAdapterOptions options = new();
        configure.Configure(options);

        Assert.NotNull(options.ServerCertificateSelector);
        Assert.Equal(expected, options.ClientCertificateMode);
    }

    [Fact]
    public void GivenNullOptions_WhenConfiguringCertificateAuthenticationOptions_ThenThrowArgumentNullException()
    {
        using TlsConfigure configure = new(Options.Create(new TlsClientOptions()), Options.Create(new TlsServerOptions()), _loggerFactory);
        _ = Assert.Throws<ArgumentNullException>(() => configure.Configure((CertificateAuthenticationOptions)null!));
        _ = Assert.Throws<ArgumentNullException>(() => configure.Configure("foo", null!));
    }

    [Fact]
    public void GivenInvalidName_WhenConfiguringCertificateAuthenticationOptions_ThenSkipConfiguring()
    {
        TlsClientOptions clientOptions = new() { CaCertificatePath = _caCertPath };
        TlsServerOptions serverOptions = new() { CertificatePath = _serverCertPath, KeyPath = _serverKeyPath };
        using TlsConfigure configure = new(Options.Create(clientOptions), Options.Create(serverOptions), _loggerFactory);

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
    public void GivenUnsafeTls_WhenConfiguringCertificateAuthenticationOptions_ThenSkipCustomCertificate(bool specifyServerCert, bool validateClientCert, bool customCertAuthority)
    {
        TlsClientOptions clientOptions = new() { CaCertificatePath = customCertAuthority ? _caCertPath : null, ValidateCertificate = validateClientCert };
        TlsServerOptions serverOptions = specifyServerCert ? new() { CertificatePath = _serverCertPath, KeyPath = _serverKeyPath } : new();
        using TlsConfigure configure = new(Options.Create(clientOptions), Options.Create(serverOptions), _loggerFactory);

        CertificateAuthenticationOptions options = new();
        configure.Configure(CertificateAuthenticationDefaults.AuthenticationScheme, options);

        Assert.Equal(X509ChainTrustMode.System, options.ChainTrustValidationMode);
        Assert.Empty(options.CustomTrustStore);
    }

    [Fact]
    public void GivenExpectedNameAndCustomCa_WhenConfiguringCertificateAuthenticationOptions_ThenUpdateOptions()
    {
        TlsClientOptions clientOptions = new() { CaCertificatePath = _caCertPath };
        TlsServerOptions serverOptions = new() { CertificatePath = _serverCertPath, KeyPath = _serverKeyPath };
        using TlsConfigure configure = new(Options.Create(clientOptions), Options.Create(serverOptions), _loggerFactory);

        CertificateAuthenticationOptions options = new();

        Assert.Equal(X509ChainTrustMode.System, options.ChainTrustValidationMode);
        configure.Configure(CertificateAuthenticationDefaults.AuthenticationScheme, options);

        Assert.Equal(X509ChainTrustMode.CustomRootTrust, options.ChainTrustValidationMode);
        X509Certificate2 actual = Assert.Single(options.CustomTrustStore);
        Assert.Equal(_caCertificate.Thumbprint, actual.Thumbprint);
    }

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public void GivenNoCustomCa_WhenMonitoringChangesForOptions_ThenReturnNullToken(bool specifyServerCert, bool validateClientCert, bool customCertAuthority)
    {
        TlsClientOptions clientOptions = new() { CaCertificatePath = customCertAuthority ? _caCertPath : null, ValidateCertificate = validateClientCert };
        TlsServerOptions serverOptions = specifyServerCert ? new() { CertificatePath = _serverCertPath, KeyPath = _serverKeyPath } : new();
        using TlsConfigure configure = new(Options.Create(clientOptions), Options.Create(serverOptions), _loggerFactory);

        Assert.Equal(CertificateAuthenticationDefaults.AuthenticationScheme, ((IOptionsChangeTokenSource<CertificateAuthenticationOptions>)configure).Name);

        IChangeToken actual = configure.GetChangeToken();
        Assert.Same(NullChangeToken.Singleton, actual);
    }

    [Fact]
    public void GivenCustomCa_WhenMonitoringChangesForOptions_ThenReturnVaidToken()
    {
        TlsClientOptions clientOptions = new() { CaCertificatePath = _caCertPath };
        TlsServerOptions serverOptions = new() { CertificatePath = _serverCertPath, KeyPath = _serverKeyPath };
        using TlsConfigure configure = new(Options.Create(clientOptions), Options.Create(serverOptions), _loggerFactory);

        Assert.Equal(CertificateAuthenticationDefaults.AuthenticationScheme, ((IOptionsChangeTokenSource<CertificateAuthenticationOptions>)configure).Name);

        IChangeToken actual = configure.GetChangeToken();
        Assert.NotSame(NullChangeToken.Singleton, actual);
    }
}
