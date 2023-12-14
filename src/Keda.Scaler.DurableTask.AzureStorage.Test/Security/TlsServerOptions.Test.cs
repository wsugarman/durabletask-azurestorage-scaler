// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Keda.Scaler.DurableTask.AzureStorage.DataAnnotations;
using Keda.Scaler.DurableTask.AzureStorage.Security;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Security;

public sealed class TlsServerOptionsTest : IDisposable
{
    private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private static readonly IServiceProvider Services = new ServiceCollection().BuildServiceProvider();

    public TlsServerOptionsTest()
        => Directory.CreateDirectory(_tempFolder);

    public void Dispose()
        => Directory.Delete(_tempFolder, true);

    [Fact]
    public void GivenMissingCertificateFile_WhenValidatingTlsServerOptions_ThenThrowValidationException()
    {
        TlsServerOptions options = new() { CertificatePath = Path.Combine(_tempFolder, "example.crt") };
        _ = Assert.Throws<ValidationException>(() => options.ThrowIfInvalid(Services));
    }

    [Fact]
    public void GivenMissingKeyFile_WhenValidatingTlsServerOptions_ThenThrowValidationException()
    {
        const string CertName = "example.crt";
        const string KeyName = "example.key";
        string certPath = Path.Combine(_tempFolder, CertName);
        string keyPath = Path.Combine(_tempFolder, KeyName);

        File.WriteAllText(certPath, "Hello world!");

        TlsServerOptions options = new()
        {
            CertificatePath = certPath,
            KeyPath = keyPath
        };

        _ = Assert.Throws<ValidationException>(() => options.ThrowIfInvalid(Services));
    }

    [Fact]
    public void GivenKeyFileWithoutCertificate_WhenValidatingTlsServerOptions_ThenThrowValidationException()
    {
        const string KeyName = "example.key";
        string keyPath = Path.Combine(_tempFolder, KeyName);
        File.WriteAllText(keyPath, "Hello world!");

        TlsServerOptions options = new() { KeyPath = keyPath };
        _ = Assert.Throws<ValidationException>(() => options.ThrowIfInvalid(Services));
    }

    [Fact]
    public void GivenValidCertificateFileOnly_WhenValidatingTlsServerOptions_ThenSucceedValidation()
    {
        const string CertName = "example.crt";
        string certPath = Path.Combine(_tempFolder, CertName);
        File.WriteAllText(certPath, "Hello world!");

        TlsServerOptions options = new() { CertificatePath = certPath };
        _ = options.ThrowIfInvalid(Services);
    }

    [Fact]
    public void GivenValidCertificateAndKeyFiles_WhenValidatingTlsServerOptions_ThenSucceedValidation()
    {
        const string CertName = "example.crt";
        const string KeyName = "example.key";
        string certPath = Path.Combine(_tempFolder, CertName);
        string keyPath = Path.Combine(_tempFolder, KeyName);

        File.WriteAllText(certPath, "Hello world!");
        File.WriteAllText(keyPath, "Hello world!");

        TlsServerOptions options = new()
        {
            CertificatePath = certPath,
            KeyPath = keyPath
        };

        _ = options.ThrowIfInvalid(Services);
    }
}
