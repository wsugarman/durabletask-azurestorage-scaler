// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.IO;
using Keda.Scaler.DurableTask.AzureStorage.Security;
using Xunit;
using Xunit.Abstractions;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Security;

public class TlsClientOptionsTest(ITestOutputHelper outputHelper) : FileSystemTest(outputHelper)
{
    [Fact]
    public void GivenMissingCertificateFile_WhenValidatingTlsClientOptions_ThenFailValidation()
    {
        TlsClientOptions options = new() { CaCertificatePath = Path.Combine(RootFolder, "example.crt") };
        Assert.True(new ValidateTlsClientOptions().Validate(null, options).Failed);
    }

    [Fact]
    public void GivenValidCertificate_WhenValidatingTlsClientOptions_ThenSucceedValidation()
    {
        const string CertName = "example.crt";
        string certPath = Path.Combine(RootFolder, CertName);
        File.WriteAllText(certPath, "Hello world!");

        TlsClientOptions options = new() { CaCertificatePath = certPath };
        Assert.True(new ValidateTlsClientOptions().Validate(null, options).Succeeded);
    }
}
