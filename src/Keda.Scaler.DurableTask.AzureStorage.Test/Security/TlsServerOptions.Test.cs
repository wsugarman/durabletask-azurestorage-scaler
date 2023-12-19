// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.IO;
using Keda.Scaler.DurableTask.AzureStorage.Security;
using Xunit;
using Xunit.Abstractions;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Security;

public class TlsServerOptionsTest(ITestOutputHelper outputHelper) : FileSystemTest(outputHelper)
{
    [Fact]
    public void GivenMissingCertificateFile_WhenValidatingTlsServerOptions_ThenFailValidation()
    {
        TlsServerOptions options = new() { CertificatePath = Path.Combine(RootFolder, "example.crt") };
        Assert.True(new ValidateTlsServerOptions().Validate(null, options).Failed);
    }

    [Fact]
    public void GivenMissingKeyFile_WhenValidatingTlsServerOptions_ThenFailValidation()
    {
        const string CertName = "example.crt";
        const string KeyName = "example.key";
        string certPath = Path.Combine(RootFolder, CertName);
        string keyPath = Path.Combine(RootFolder, KeyName);

        File.WriteAllText(certPath, "Hello world!");

        TlsServerOptions options = new()
        {
            CertificatePath = certPath,
            KeyPath = keyPath
        };

        Assert.True(new ValidateTlsServerOptions().Validate(null, options).Failed);
    }

    [Fact]
    public void GivenKeyFileWithoutCertificate_WhenValidatingTlsServerOptions_ThenFailValidation()
    {
        const string KeyName = "example.key";
        string keyPath = Path.Combine(RootFolder, KeyName);
        File.WriteAllText(keyPath, "Hello world!");

        TlsServerOptions options = new() { KeyPath = keyPath };
        Assert.True(new ValidateTlsServerOptions().Validate(null, options).Failed);
    }

    [Fact]
    public void GivenValidCertificateFileOnly_WhenValidatingTlsServerOptions_ThenSucceedValidation()
    {
        const string CertName = "example.crt";
        string certPath = Path.Combine(RootFolder, CertName);
        File.WriteAllText(certPath, "Hello world!");

        TlsServerOptions options = new() { CertificatePath = certPath };
        Assert.True(new ValidateTlsServerOptions().Validate(null, options).Succeeded);
    }

    [Fact]
    public void GivenValidCertificateAndKeyFiles_WhenValidatingTlsServerOptions_ThenSucceedValidation()
    {
        const string CertName = "example.crt";
        const string KeyName = "example.key";
        string certPath = Path.Combine(RootFolder, CertName);
        string keyPath = Path.Combine(RootFolder, KeyName);

        File.WriteAllText(certPath, "Hello world!");
        File.WriteAllText(keyPath, "Hello world!");

        TlsServerOptions options = new()
        {
            CertificatePath = certPath,
            KeyPath = keyPath
        };

        Assert.True(new ValidateTlsServerOptions().Validate(null, options).Succeeded);
    }
}
