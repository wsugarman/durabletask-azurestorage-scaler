// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Keda.Scaler.DurableTask.AzureStorage.Security;
using Keda.Scaler.DurableTask.AzureStorage.Test.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Xunit;
using Xunit.Abstractions;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Security;

public sealed class CertificateFileMonitorTest : IDisposable
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public CertificateFileMonitorTest(ITestOutputHelper outputHelper)
    {
        _loggerFactory = XUnitLogger.CreateFactory(outputHelper);
        _logger = _loggerFactory.CreateLogger(LogCategories.Security);
        _ = Directory.CreateDirectory(_tempFolder);
    }

    public void Dispose()
    {
        _loggerFactory.Dispose();
        Directory.Delete(_tempFolder, true);
    }

    [Fact]
    public void GivenNullCertificateFile_WhenCreatingMonitor_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new CertificateFileMonitor(null!, _logger));

    [Fact]
    public void GivenNullLogger_WhenCreatingMonitor_ThenThrowArgumentNullException()
    {
        using CertificateFile file = new(Path.Combine(_tempFolder, "unused.crt"));
        _ = Assert.Throws<ArgumentNullException>(() => new CertificateFileMonitor(file, null!));
    }

    [Fact]
    public async Task GivenChangingToInvalidFile_WhenMonitoringCertificateFile_ThenAutomaticallyUpdateValue()
    {
        const string CertName = "example.crt";
        string certPath = Path.Combine(_tempFolder, CertName);

        using RSA key = RSA.Create();
        using X509Certificate2 cert = key.CreateSelfSignedCertificate();
        await File.WriteAllBytesAsync(certPath, cert.Export(X509ContentType.Pkcs12));

        using ManualResetEventSlim changeEvent = new(initialState: false);
        using CertificateFile certificateFile = new(certPath);
        using CertificateFileMonitor monitor = certificateFile.Monitor(_logger);
        using IDisposable subscription = ChangeToken.OnChange(monitor.GetReloadToken, changeEvent.Set);

        // Ensure the certificate is originally as expected
        Assert.False(changeEvent.IsSet);
        Assert.Equal(cert.Thumbprint, monitor.Current.Thumbprint);

        // Edit the file and check that an error is re-thrown from the update thread
        await File.WriteAllTextAsync(certPath, "Hello world!");
        Assert.True(changeEvent.Wait(TimeSpan.FromSeconds(30)));
        await Task.Delay(TimeSpan.FromSeconds(5)); // Allow superfluous (for our purpose) file system events to fire

        _ = Assert.ThrowsAny<CryptographicException>(() => monitor.Current);

        changeEvent.Reset();

        // Fix the file
        await File.WriteAllBytesAsync(certPath, cert.Export(X509ContentType.Pkcs12));
        Assert.True(changeEvent.Wait(TimeSpan.FromSeconds(30)));
        Assert.Equal(cert.Thumbprint, monitor.Current.Thumbprint);
    }

    [Fact]
    public async Task GivenChangingFile_WhenMonitoringCertificateFile_ThenAutomaticallyUpdateValue()
    {
        const string CertName = "example.crt";
        string certPath = Path.Combine(_tempFolder, CertName);

        using RSA originalKey = RSA.Create();
        using X509Certificate2 originalCert = originalKey.CreateSelfSignedCertificate();
        await File.WriteAllBytesAsync(certPath, originalCert.Export(X509ContentType.Pkcs12));

        using ManualResetEventSlim changeEvent = new(initialState: false);
        using CertificateFile certificateFile = new(certPath);
        using CertificateFileMonitor monitor = certificateFile.Monitor(_logger);
        using IDisposable subscription = ChangeToken.OnChange(monitor.GetReloadToken, changeEvent.Set);

        // Set up some task to simulate a concurrent consumer
        using CancellationTokenSource tokenSource = new();
        Task consumer = Task.Run(() => GetCurrentCert(monitor, tokenSource.Token));

        Assert.False(changeEvent.IsSet);
        Assert.Equal(originalCert.Thumbprint, monitor.Current.Thumbprint);

        try
        {
            // Continue to edit the certificate multiple times
            for (int i = 1; i <= 10; i++)
            {
                // Edit the file and check the new value
                using RSA newKey = RSA.Create();
                using X509Certificate2 newCert = newKey.CreateSelfSignedCertificate();
                await File.WriteAllBytesAsync(certPath, newCert.Export(X509ContentType.Pkcs12));

                Assert.True(changeEvent.Wait(TimeSpan.FromSeconds(30)), $"[{DateTime.UtcNow}] Failed to update on attempt #{i}");
                await Task.Delay(TimeSpan.FromSeconds(5)); // Allow superfluous (for our purpose) file system events to fire

                Assert.Equal(newCert.Thumbprint, monitor.Current.Thumbprint);

                changeEvent.Reset();
            }
        }
        finally
        {
            await tokenSource.CancelAsync();
        }

        await consumer;

        static void GetCurrentCert(CertificateFileMonitor m, CancellationToken t)
        {
            while (!t.IsCancellationRequested)
            {
                Assert.NotNull(m.Current);
                _ = Thread.Yield();
            }
        }
    }
}
