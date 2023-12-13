// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Keda.Scaler.DurableTask.AzureStorage.Security;
using Microsoft.Extensions.Primitives;
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
        expected.WriteFile(certPath);

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
        expected.WriteFile(certPath);
        key.WriteFile(keyPath);

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
        expected.WriteFile(key, certPath);

        using CertificateFile certificateFile = CertificateFile.CreateFromPemFile(certPath);
        using X509Certificate2 actual = certificateFile.Load();

        Assert.Equal(certPath, certificateFile.Path);
        Assert.Null(certificateFile.KeyPath);
        Assert.Equal(expected.Thumbprint, actual.Thumbprint);
    }

    [Fact]
    public void GivenChangingFile_WhenWatchingCertificateFile_ThenSuccessfullyNotifySubscribers()
    {
        const string CertName = "example.crt";
        string certPath = Path.Combine(_tempFolder, CertName);

        using RSA key1 = RSA.Create();
        using X509Certificate2 cert1 = key1.CreateSelfSignedCertificate();
        cert1.WriteFile(certPath);

        using ManualResetEventSlim changeEvent = new(initialState: false);
        using CertificateFile certificateFile = new(certPath);
        using IDisposable subscription = ChangeToken.OnChange(certificateFile.Watch, changeEvent.Set);

        // Ensure the certificate is originally as expected
        Assert.False(changeEvent.IsSet);
        using X509Certificate2 actual1 = certificateFile.Load();
        Assert.Equal(cert1.Thumbprint, actual1.Thumbprint);

        // Edit the file and check the new value
        using RSA key2 = RSA.Create();
        using X509Certificate2 cert2 = key2.CreateSelfSignedCertificate();
        cert2.WriteFile(certPath + ".tmp");
        File.Move(certPath + ".tmp", certPath, overwrite: true);

        Assert.True(changeEvent.Wait(TimeSpan.FromSeconds(10)));
        using X509Certificate2 actual2 = certificateFile.Load();
        Assert.Equal(cert2.Thumbprint, actual2.Thumbprint);
    }
}
