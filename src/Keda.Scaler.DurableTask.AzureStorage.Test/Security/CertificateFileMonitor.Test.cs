// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Keda.Scaler.DurableTask.AzureStorage.Security;
using Microsoft.Extensions.Primitives;
using Xunit;
using Xunit.Abstractions;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Security;

public class CertificateFileMonitorTest(ITestOutputHelper outputHelper) : FileSystemTest(outputHelper)
{
    [Fact]
    public void GivenNullCertificateFile_WhenCreatingMonitor_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new CertificateFileMonitor(null!, Logger));

    [Fact]
    public void GivenNullLogger_WhenCreatingMonitor_ThenThrowArgumentNullException()
    {
        using CertificateFile file = new(Path.Combine(RootFolder, "unused.crt"));
        _ = Assert.Throws<ArgumentNullException>(() => new CertificateFileMonitor(file, null!));
    }

    [Fact]
    public async Task GivenChangingToInvalidFile_WhenMonitoringCertificateFile_ThenAutomaticallyUpdateValue()
    {
        const string CertName = "example.crt";
        string certPath = Path.Combine(RootFolder, CertName);

        using RSA key = RSA.Create();
        using X509Certificate2 cert = key.CreateSelfSignedCertificate();
        await File.WriteAllBytesAsync(certPath, cert.Export(X509ContentType.Pkcs12));

        using ManualResetEventSlim changeEvent = new(initialState: false);
        using CertificateFile certificateFile = new(certPath);
        using CertificateFileMonitor monitor = certificateFile.Monitor(Logger);
        using IDisposable subscription = ChangeToken.OnChange(monitor.GetReloadToken, changeEvent.Set);

        // Ensure the certificate is originally as expected
        Assert.False(changeEvent.IsSet);
        Assert.Equal(cert.Thumbprint, monitor.Current.Thumbprint);

        // Edit the file and check that an error is re-thrown from the update thread
        await File.WriteAllTextAsync(certPath, "Hello world!");

        using CancellationTokenSource tokenSource = new();
        tokenSource.CancelAfter(TimeSpan.FromSeconds(15));
        await monitor.WaitForExceptionAsync(TimeSpan.FromMilliseconds(100), tokenSource.Token);
    }
}
