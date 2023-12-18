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
using Keda.Scaler.DurableTask.AzureStorage.Test.IO;
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
        Assert.True(changeEvent.Wait(FileSystem.PollingIntervalMs * 3));
        await Task.Delay(FileSystem.PollingIntervalMs * 3); // Allow superfluous (for our purpose) file system events to fire

        _ = Assert.ThrowsAny<CryptographicException>(() => monitor.Current);

        changeEvent.Reset();

        // Fix the file
        await File.WriteAllBytesAsync(certPath, cert.Export(X509ContentType.Pkcs12));
        Assert.True(changeEvent.Wait(FileSystem.PollingIntervalMs * 3));
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

        Assert.False(changeEvent.IsSet);
        Assert.Equal(originalCert.Thumbprint, monitor.Current.Thumbprint);

        // Continue to edit the certificate multiple times
        for (int i = 1; i <= 10; i++)
        {
            // Edit the file and check the new value
            using RSA newKey = RSA.Create();
            using X509Certificate2 newCert = newKey.CreateSelfSignedCertificate();
            await File.WriteAllBytesAsync(certPath, newCert.Export(X509ContentType.Pkcs12));

            Assert.True(changeEvent.Wait(FileSystem.PollingIntervalMs * 3), $"[{DateTime.UtcNow}] Failed to update on attempt #{i}");
            await Task.Delay(FileSystem.PollingIntervalMs * 3); // Allow superfluous (for our purpose) file system events to fire

            Assert.Equal(newCert.Thumbprint, monitor.Current.Thumbprint);

            changeEvent.Reset();
        }
    }

    [Fact]
    public async Task GivenConcurrentReaders_WhenMonitoringCertificateFile_ThenEventuallyShowConsistency()
    {
        const string CertName = "example.crt";
        string certPath = Path.Combine(_tempFolder, CertName);

        using RSA originalKey = RSA.Create();
        using X509Certificate2 originalCert = originalKey.CreateSelfSignedCertificate();
        await File.WriteAllBytesAsync(certPath, originalCert.Export(X509ContentType.Pkcs12));

        using CertificateFile certificateFile = new(certPath);
        using CertificateFileMonitor monitor = certificateFile.Monitor(_logger);
        Assert.Equal(originalCert.Thumbprint, monitor.Current.Thumbprint);

        using CancellationTokenSource tokenSource = new();
        Task[] consumers = Enumerable
            .Repeat(monitor, 10)
            .Select(m => PollCurrentAsync(m, TimeSpan.FromMilliseconds(500), tokenSource.Token))
            .ToArray();

        // Continue to edit the certificate multiple times
        string expected = "";
        for (int i = 0; i < 3; i++)
        {
            // Edit the file and check the new value
            using RSA newKey = RSA.Create();
            using X509Certificate2 newCert = newKey.CreateSelfSignedCertificate();
            await File.WriteAllBytesAsync(certPath, newCert.Export(X509ContentType.Pkcs12));
            await Task.Delay(FileSystem.PollingIntervalMs * 5); // Allow superfluous (for our purpose) file system events to fire

            expected = newCert.Thumbprint;
        }

        // Assert that the certificate is eventually correct
        await Task.Delay(FileSystem.PollingIntervalMs * 5);
        Assert.Equal(expected, monitor.Current.Thumbprint);

        await tokenSource.CancelAsync();
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Task.WhenAll(consumers));

        static async Task PollCurrentAsync(CertificateFileMonitor monitor, TimeSpan delay, CancellationToken token)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                _ = monitor.Current;
                await Task.Delay(delay, token);
            }
        }
    }
}
