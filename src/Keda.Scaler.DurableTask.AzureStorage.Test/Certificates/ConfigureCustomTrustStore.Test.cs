// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Keda.Scaler.DurableTask.AzureStorage.Certificates;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.Extensions.Options;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Certificates;

[Collection(nameof(CertificateTestCollection))]
public class ConfigureCustomTrustStoreTest : IAsyncLifetime
{
    private readonly string _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public ValueTask InitializeAsync()
    {
        _ = Directory.CreateDirectory(_testDirectory);
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        Directory.Delete(_testDirectory, recursive: true);
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    [Fact]
    public void GivenNullOptions_WhenCreatingConfigure_ThenThrowArgumentNullException()
    {
        using ReaderWriterLockSlim readerWriterLock = new();
        _ = Assert.Throws<ArgumentNullException>(() => new ConfigureCustomTrustStore(null!, readerWriterLock));

        IOptions<ClientCertificateValidationOptions> options = Options.Create<ClientCertificateValidationOptions>(null!);
        _ = Assert.Throws<ArgumentNullException>(() => new ConfigureCustomTrustStore(options, readerWriterLock));

        options = Options.Create(new ClientCertificateValidationOptions { CertificateAuthority = null });
        _ = Assert.Throws<ArgumentNullException>(() => new ConfigureCustomTrustStore(options, readerWriterLock));
    }

    [Fact]
    public void GivenNullReaderWriterLock_WhenCreatingConfigure_ThenThrowArgumentNullException()
    {
        IOptions<ClientCertificateValidationOptions> options = Options.Create(new ClientCertificateValidationOptions { CertificateAuthority = new CaCertificateFileOptions() });
        _ = Assert.Throws<ArgumentNullException>(() => new ConfigureCustomTrustStore(options, null!));
    }

    [Fact]
    public async ValueTask GivenCertificate_WhenConfiguringOptions_ThenSetCustomTrustStore()
    {
        // Create the certificate and write to disk
        const string CertName = "example.crt";
        string certPath = Path.Combine(_testDirectory, CertName);

        using RSA key = RSA.Create();
        using X509Certificate2 expected = key.CreateSelfSignedCertificate();
        await File.WriteAllTextAsync(certPath, expected.ExportCertificatePem(), TestContext.Current.CancellationToken);

        // Configure the options
        using ReaderWriterLockSlim readerWriterLock = new();
        ClientCertificateValidationOptions validationOptions = new()
        {
            CertificateAuthority = new CaCertificateFileOptions { Path = certPath },
        };

        CertificateAuthenticationOptions options = new();
        using ConfigureCustomTrustStore configure = new(Options.Create(validationOptions), readerWriterLock);
        configure.Configure(options);

        X509Certificate2 actual = Assert.Single(options.CustomTrustStore);
        Assert.Equal(expected.Thumbprint, actual.Thumbprint);
        Assert.Equal(X509ChainTrustMode.CustomRootTrust, options.ChainTrustValidationMode);
    }

    [Fact(Timeout = 1000 * 10)]
    public async ValueTask GivenCertificateFileChange_WhenConfiguringOptions_ThenUpdateCustomTrustStore()
    {
        // Create the certificate and write to disk
        const string CertName = "example.crt";
        string certPath = Path.Combine(_testDirectory, CertName);

        using RSA key1 = RSA.Create();
        using X509Certificate2 expected1 = key1.CreateSelfSignedCertificate();
        await File.WriteAllTextAsync(certPath, expected1.ExportCertificatePem(), TestContext.Current.CancellationToken);

        // Configure the options
        using ReaderWriterLockSlim readerWriterLock = new();
        ClientCertificateValidationOptions validationOptions = new()
        {
            CertificateAuthority = new CaCertificateFileOptions { Path = certPath },
        };

        CertificateAuthenticationOptions options = new();
        using ConfigureCustomTrustStore configure = new(Options.Create(validationOptions), readerWriterLock);
        configure.Configure(options);

        X509Certificate2 actual = Assert.Single(options.CustomTrustStore);
        Assert.Equal(expected1.Thumbprint, actual.Thumbprint);
        Assert.Equal(X509ChainTrustMode.CustomRootTrust, options.ChainTrustValidationMode);

        // Update the certificate file after a half second
        await Task.Delay(500, TestContext.Current.CancellationToken);

        using RSA key2 = RSA.Create();
        using X509Certificate2 expected2 = key2.CreateSelfSignedCertificate();
        await File.WriteAllTextAsync(certPath, expected2.ExportCertificatePem(), TestContext.Current.CancellationToken);

        // Check for the updated certificate
        do
        {
            configure.Configure(options);
            actual = Assert.Single(options.CustomTrustStore);
        } while (actual.Thumbprint != expected2.Thumbprint);

        Assert.Equal(X509ChainTrustMode.CustomRootTrust, options.ChainTrustValidationMode);
    }
}
