// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Keda.Scaler.DurableTask.AzureStorage.Security;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Security;

public sealed class CertificateFileMonitorTest : IDisposable
{
    private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public CertificateFileMonitorTest()
        => Directory.CreateDirectory(_tempFolder);

    public void Dispose()
        => Directory.Delete(_tempFolder, true);

    [Fact]
    public void GivenNullCertificateFile_WhenCreatingMonitor_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => CertificateFileMonitor.Create(null!));

    [Fact]
    public void GivenChangingToInvalidFile_WhenMonitoringCertificateFile_ThenAutomaticallyUpdateValue()
    {
        const string CertName = "example.crt";
        string certPath = Path.Combine(_tempFolder, CertName);

        using RSA key1 = RSA.Create();
        using X509Certificate2 cert1 = key1.CreateSelfSignedCertificate();
        cert1.WriteFile(certPath);

        using ManualResetEventSlim changeEvent = new(initialState: false);
        using CertificateFile certificateFile = new(certPath);
        using CertificateFileMonitor monitor = certificateFile.Monitor();
        using IDisposable subscription = ChangeToken.OnChange(monitor.GetReloadToken, changeEvent.Set);

        // Ensure the certificate is originally as expected
        Assert.False(changeEvent.IsSet);
        Assert.Equal(cert1.Thumbprint, monitor.Current.Thumbprint);

        // Edit the file and check the new value
        File.WriteAllText(certPath + ".tmp", "Hello world!");
        File.Move(certPath + ".tmp", certPath, overwrite: true);

        Assert.True(changeEvent.Wait(TimeSpan.FromSeconds(10)));
        _ = Assert.ThrowsAny<CryptographicException>(() => monitor.Current);
    }

    [Fact]
    public async void GivenChangingFile_WhenMonitoringCertificateFile_ThenAutomaticallyUpdateValue()
    {
        const string CertName = "example.crt";
        string certPath = Path.Combine(_tempFolder, CertName);

        using RSA key1 = RSA.Create();
        using X509Certificate2 cert1 = key1.CreateSelfSignedCertificate();
        cert1.WriteFile(certPath);

        using ManualResetEventSlim changeEvent = new(initialState: false);
        using CertificateFile certificateFile = new(certPath);
        using CertificateFileMonitor monitor = certificateFile.Monitor();
        using IDisposable subscription = ChangeToken.OnChange(monitor.GetReloadToken, changeEvent.Set);

        // Set up some task to simulate concurrent consumers
        using CancellationTokenSource tokenSource = new();
        Task[] consumers = Enumerable
            .Range(0, 25)
            .Select(x => Task.Run(() => GetCurrentCert(monitor, tokenSource.Token)))
            .ToArray();
        try
        {
            // Ensure the certificate is originally as expected
            Assert.False(changeEvent.IsSet);
            Assert.Equal(cert1.Thumbprint, monitor.Current.Thumbprint);

            // Edit the file and check the new value
            using RSA key2 = RSA.Create();
            using X509Certificate2 cert2 = key2.CreateSelfSignedCertificate();
            cert2.WriteFile(certPath + ".tmp");
            File.Move(certPath + ".tmp", certPath, overwrite: true);

            Assert.True(changeEvent.Wait(TimeSpan.FromSeconds(10)));
            Assert.Equal(cert2.Thumbprint, monitor.Current.Thumbprint);
        }
        finally
        {
            tokenSource.Cancel();
        }

        await Task.WhenAll(consumers);

        static void GetCurrentCert(CertificateFileMonitor m, CancellationToken t)
        {
            while (!t.IsCancellationRequested)
                Assert.NotNull(m.Current);
        }
    }
}
