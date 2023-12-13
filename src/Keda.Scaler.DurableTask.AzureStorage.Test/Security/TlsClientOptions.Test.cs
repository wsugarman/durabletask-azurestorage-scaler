// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Keda.Scaler.DurableTask.AzureStorage.ComponentModel.DataAnnotations;
using Keda.Scaler.DurableTask.AzureStorage.Security;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Security;

public sealed class TlsClientOptionsTest : IDisposable
{
    private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private static readonly IServiceProvider Services = new ServiceCollection().BuildServiceProvider();

    public TlsClientOptionsTest()
        => Directory.CreateDirectory(_tempFolder);

    public void Dispose()
        => Directory.Delete(_tempFolder, true);

    [Fact]
    public void GivenMissingCertificateFile_WhenValidatingTlsClientOptions_ThenThrowValidationException()
    {
        TlsClientOptions options = new() { CaCertificatePath = Path.Combine(_tempFolder, "example.crt") };
        _ = Assert.Throws<ValidationException>(() => ObjectValidator.ThrowIfInvalid(options, Services));
    }

    [Fact]
    public void GivenValidCertificate_WhenValidatingTlsClientOptions_ThenSucceedValidation()
    {
        const string CertName = "example.crt";
        string certPath = Path.Combine(_tempFolder, CertName);
        File.WriteAllText(certPath, "Hello world!");

        TlsClientOptions options = new() { CaCertificatePath = certPath };
        ObjectValidator.ThrowIfInvalid(options, Services);
    }
}
