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
using Xunit.Abstractions;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Security;

public class CertificateFileTest(ITestOutputHelper outputHelper) : FileSystemTest(outputHelper)
{
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
        string certPath = Path.Combine(RootFolder, CertName);

        using CertificateFile certificateFile = new(certPath);

        Assert.False(certificateFile.EnableRaisingEvents);
        Assert.Equal(certPath, certificateFile.Path);
        Assert.Null(certificateFile.KeyPath);
        _ = Assert.ThrowsAny<CryptographicException>(certificateFile.Load);
    }

    [Fact]
    public void GivenInvalidCertificate_WhenLoadingCertificateFile_ThenThrowCryptographicException()
    {
        const string FileName = "hello.txt";
        string filePath = Path.Combine(RootFolder, FileName);
        File.WriteAllText(filePath, "Hello world!");

        using CertificateFile certificateFile = new(filePath);

        Assert.False(certificateFile.EnableRaisingEvents);
        Assert.Equal(filePath, certificateFile.Path);
        Assert.Null(certificateFile.KeyPath);
        _ = Assert.ThrowsAny<CryptographicException>(certificateFile.Load);
    }

    [Fact]
    public void GivenCertificate_WhenLoadingCertificateFile_ThenSuccessfullyLoadFromDisk()
    {
        const string CertName = "example.crt";
        string certPath = Path.Combine(RootFolder, CertName);

        using RSA key = RSA.Create();
        using X509Certificate2 expected = key.CreateSelfSignedCertificate();
        File.WriteAllBytes(certPath, expected.Export(X509ContentType.Pkcs12));

        using CertificateFile certificateFile = new(certPath);
        using X509Certificate2 actual = certificateFile.Load();

        Assert.False(certificateFile.EnableRaisingEvents);
        Assert.Equal(certPath, certificateFile.Path);
        Assert.Null(certificateFile.KeyPath);
        Assert.Equal(expected.Thumbprint, actual.Thumbprint);
    }

    [Fact]
    public void GivenPemFile_WhenLoadingCertificateFile_ThenSuccessfullyLoadFromDisk()
    {
        const string CertName = "example.crt";
        const string KeyName = "example.key";
        string certPath = Path.Combine(RootFolder, CertName);
        string keyPath = Path.Combine(RootFolder, KeyName);

        using RSA key = RSA.Create();
        using X509Certificate2 expected = key.CreateSelfSignedCertificate();
        File.WriteAllText(certPath, expected.ExportCertificatePem());
        File.WriteAllText(keyPath, key.ExportRSAPrivateKeyPem());

        using CertificateFile certificateFile = CertificateFile.CreateFromPemFile(certPath, keyPath);
        using X509Certificate2 actual = certificateFile.Load();

        Assert.False(certificateFile.EnableRaisingEvents);
        Assert.Equal(certPath, certificateFile.Path);
        Assert.Equal(keyPath, certificateFile.KeyPath);
        Assert.Equal(expected.Thumbprint, actual.Thumbprint);
    }

    [Fact]
    public void GivenCombinedPemFile_WhenLoadingCertificateFile_ThenSuccessfullyLoadFromDisk()
    {
        const string CertName = "example.crt";
        string certPath = Path.Combine(RootFolder, CertName);

        using RSA key = RSA.Create();
        using X509Certificate2 expected = key.CreateSelfSignedCertificate();
        File.WriteAllText(certPath, expected.ExportCertificatePem(key));

        using CertificateFile certificateFile = CertificateFile.CreateFromPemFile(certPath);
        using X509Certificate2 actual = certificateFile.Load();

        Assert.False(certificateFile.EnableRaisingEvents);
        Assert.Equal(certPath, certificateFile.Path);
        Assert.Null(certificateFile.KeyPath);
        Assert.Equal(expected.Thumbprint, actual.Thumbprint);
    }

    [Fact]
    public async Task GivenNoSubscriber_WhenChangingCertificateFile_ThenSkipAlerting()
    {
        const string CertName = "example.crt";
        string certPath = Path.Combine(RootFolder, CertName);

        using RSA key = RSA.Create();
        using X509Certificate2 cert = key.CreateSelfSignedCertificate();
        await File.WriteAllTextAsync(certPath, cert.ExportCertificatePem(key));

        using ManualResetEventSlim changeEvent = new(initialState: false);
        using CertificateFile certificateFile = CertificateFile.CreateFromPemFile(certPath);
        using X509Certificate2 actual1 = certificateFile.Load();

        // This event is for the underlying file system and is not exposed to users
        certificateFile.FileSystemChanged += Set;
        certificateFile.EnableRaisingEvents = true;

        Assert.True(certificateFile.EnableRaisingEvents);
        Assert.Equal(certPath, certificateFile.Path);
        Assert.Null(certificateFile.KeyPath);
        Assert.Equal(cert.Thumbprint, actual1.Thumbprint);

        // Edit the file, wait some time for the subscriber-less handler to execute, and assert the thumbprint
        File.SetLastWriteTimeUtc(certPath, DateTime.UtcNow);
        Assert.True(changeEvent.Wait(TimeSpan.FromSeconds(15)));

        certificateFile.FileSystemChanged -= Set;

        void Set(object sender, FileSystemEventArgs args)
            => changeEvent.Set();
    }

    [Fact]
    public async Task GivenModifiedFile_WhenWatchingCertificateFile_ThenSuccessfullyNotifySubscribers()
    {
        const string CertName = "example.crt";
        string certPath = Path.Combine(RootFolder, CertName);

        using RSA key = RSA.Create();
        using X509Certificate2 cert = key.CreateSelfSignedCertificate();
        await File.WriteAllBytesAsync(certPath, cert.Export(X509ContentType.Pkcs12));

        using ManualResetEventSlim changeEvent = new(initialState: false);
        using CertificateFile certificateFile = new(certPath) { EnableRaisingEvents = true };
        certificateFile.Changed += (f, args) =>
        {
            Assert.Null(args.Certificate);
            Assert.NotNull(args.Exception);

            changeEvent.Set();
        };

        // Ensure the certificate is originally as expected
        Assert.False(changeEvent.IsSet);
        using X509Certificate2 actual = certificateFile.Load();
        Assert.Equal(cert.Thumbprint, actual.Thumbprint);

        // Edit the file multiple times and check the new value
        for (int i = 1; i <= 5; i++)
        {
            File.SetLastWriteTimeUtc(certPath, DateTime.UtcNow);
            Assert.True(changeEvent.Wait(TimeSpan.FromSeconds(15)));

            using X509Certificate2 existing = certificateFile.Load();
            Assert.Equal(cert.Thumbprint, existing.Thumbprint);
        }
    }
}
