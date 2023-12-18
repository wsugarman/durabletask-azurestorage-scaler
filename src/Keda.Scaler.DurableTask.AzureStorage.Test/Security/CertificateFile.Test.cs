// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Keda.Scaler.DurableTask.AzureStorage.Security;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Security;

public sealed class CertificateFileTest : IDisposable
{
    private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public CertificateFileTest()
        => Directory.CreateDirectory(_tempFolder);

    public void Dispose()
        => Directory.Delete(_tempFolder, true);

    [Fact]
    public void GivenNullFileName_WhenCreatingCertificateFile_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new CertificateFile(null!));

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void GivenEmptyOrWhiteSpaceFileName_WhenCreatingCertificateFile_ThenThrowArgumentException(string fileName)
        => Assert.Throws<ArgumentException>(() => new CertificateFile(fileName));

    [Fact]
    public void GivenNonExistentCertificate_WhenLoadingCertificateFile_ThenThrowFileNotFound()
    {
        const string CertName = "example.crt";
        string certPath = Path.Combine(_tempFolder, CertName);

        using CertificateFile certificateFile = new(certPath);

        Assert.Equal(certPath, certificateFile.Path);
        Assert.Null(certificateFile.KeyPath);
        _ = Assert.ThrowsAny<CryptographicException>(certificateFile.Load);
    }

    [Fact]
    public void GivenInvalidCertificate_WhenLoadingCertificateFile_ThenThrowCryptographicException()
    {
        const string FileName = "hello.txt";
        string filePath = Path.Combine(_tempFolder, FileName);
        File.WriteAllText(filePath, "Hello world!");

        using CertificateFile certificateFile = new(filePath);

        Assert.Equal(filePath, certificateFile.Path);
        Assert.Null(certificateFile.KeyPath);
        _ = Assert.ThrowsAny<CryptographicException>(certificateFile.Load);
    }

    [Fact]
    public void GivenCertificate_WhenLoadingCertificateFile_ThenSuccessfullyLoadFromDisk()
    {
        const string CertName = "example.crt";
        string certPath = Path.Combine(_tempFolder, CertName);

        using RSA key = RSA.Create();
        using X509Certificate2 expected = key.CreateSelfSignedCertificate();
        File.WriteAllBytes(certPath, expected.Export(X509ContentType.Pkcs12));

        using CertificateFile certificateFile = new(certPath);
        using X509Certificate2 actual = certificateFile.Load();

        Assert.Equal(certPath, certificateFile.Path);
        Assert.Null(certificateFile.KeyPath);
        Assert.Equal(expected.Thumbprint, actual.Thumbprint);
    }

    [Fact]
    public void GivenPemFile_WhenLoadingCertificateFile_ThenSuccessfullyLoadFromDisk()
    {
        const string CertName = "example.crt";
        const string KeyName = "example.key";
        string certPath = Path.Combine(_tempFolder, CertName);
        string keyPath = Path.Combine(_tempFolder, KeyName);

        using RSA key = RSA.Create();
        using X509Certificate2 expected = key.CreateSelfSignedCertificate();
        File.WriteAllText(certPath, expected.ExportCertificatePem());
        File.WriteAllText(keyPath, key.ExportRSAPrivateKeyPem());

        using CertificateFile certificateFile = CertificateFile.CreateFromPemFile(certPath, keyPath);
        using X509Certificate2 actual = certificateFile.Load();

        Assert.Equal(certPath, certificateFile.Path);
        Assert.Equal(keyPath, certificateFile.KeyPath);
        Assert.Equal(expected.Thumbprint, actual.Thumbprint);
    }

    [Fact]
    public void GivenCombinedPemFile_WhenLoadingCertificateFile_ThenSuccessfullyLoadFromDisk()
    {
        const string CertName = "example.crt";
        string certPath = Path.Combine(_tempFolder, CertName);

        using RSA key = RSA.Create();
        using X509Certificate2 expected = key.CreateSelfSignedCertificate();
        File.WriteAllText(certPath, expected.ExportCertificatePem(key));

        using CertificateFile certificateFile = CertificateFile.CreateFromPemFile(certPath);
        using X509Certificate2 actual = certificateFile.Load();

        Assert.Equal(certPath, certificateFile.Path);
        Assert.Null(certificateFile.KeyPath);
        Assert.Equal(expected.Thumbprint, actual.Thumbprint);
    }

    [Fact]
    public async Task GivenChangingFile_WhenWatchingCertificateFile_ThenSuccessfullyNotifySubscribers()
    {
        const string CertName = "example.crt";
        string certPath = Path.Combine(_tempFolder, CertName);

        using RSA originalKey = RSA.Create();
        using X509Certificate2 originalCert = originalKey.CreateSelfSignedCertificate();
        await File.WriteAllBytesAsync(certPath, originalCert.Export(X509ContentType.Pkcs12));

        using ManualResetEventSlim changeEvent = new(initialState: false);
        using CertificateFile certificateFile = new(certPath);
        certificateFile.Changed += (f, args) =>
        {
            Assert.Null(args.Certificate);
            Assert.NotNull(args.Exception);

            changeEvent.Set();
        };

        // Ensure the certificate is originally as expected
        Assert.False(changeEvent.IsSet);
        using X509Certificate2 originalActual = certificateFile.Load();
        Assert.Equal(originalCert.Thumbprint, originalActual.Thumbprint);

        // Edit the file multiple times and check the new value
        for (int i = 1; i <= 10; i++)
        {
            using RSA newKey = RSA.Create();
            using X509Certificate2 newCert = newKey.CreateSelfSignedCertificate();
            await File.WriteAllBytesAsync(certPath, newCert.Export(X509ContentType.Pkcs12));

            Assert.True(changeEvent.Wait(TimeSpan.FromSeconds(30)), $"[{DateTime.UtcNow}] Failed to update on attempt #{i}");
            await Task.Delay(TimeSpan.FromSeconds(5)); // Allow superfluous (for our purpose) file system events to fire

            using X509Certificate2 newActual = certificateFile.Load();
            Assert.Equal(newCert.Thumbprint, newActual.Thumbprint);

            changeEvent.Reset();
        }
    }
}
