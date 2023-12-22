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

        using CancellationTokenSource tokenSource = new(TimeSpan.FromSeconds(15));
        await monitor.WaitForExceptionAsync(TimeSpan.FromMilliseconds(100), tokenSource.Token);
    }

    [Fact]
    public async Task GivenUpdatingCertificate_WhenMonitoringCertificateFile_ThenIncrementallyUpdate()
    {
        const string CertName = "example.crt";
        string certPath = Path.Combine(RootFolder, CertName);

        using RSA originalKey = RSA.Create();
        using X509Certificate2 originalCert = originalKey.CreateSelfSignedCertificate();
        await File.WriteAllBytesAsync(certPath, originalCert.Export(X509ContentType.Pkcs12));

        using CertificateFile certificateFile = new(certPath);
        using CertificateFileMonitor monitor = certificateFile.Monitor(Logger);
        Assert.Equal(originalCert.Thumbprint, monitor.Current.Thumbprint);

        for (int i = 0; i < 10; i++)
        {
            using RSA key = RSA.Create();
            using X509Certificate2 cert = key.CreateSelfSignedCertificate();
            await File.WriteAllBytesAsync(certPath, cert.Export(X509ContentType.Pkcs12));

            using CancellationTokenSource tokenSource = new(TimeSpan.FromMinutes(2));
            Task consumer = monitor.WaitForThumbprintAsync(cert.Thumbprint, TimeSpan.FromMilliseconds(200), tokenSource.Token);
            await consumer;
        }
    }

    [Fact]
    public async Task GivenConcurrentReaders_WhenMonitoringCertificateFile_ThenEventuallyShowConsistency()
    {
        const string CertName = "example.crt";
        string certPath = Path.Combine(RootFolder, CertName);

        using RSA originalKey = RSA.Create();
        using X509Certificate2 originalCert = originalKey.CreateSelfSignedCertificate();
        await File.WriteAllBytesAsync(certPath, originalCert.Export(X509ContentType.Pkcs12));

        using CertificateFile certificateFile = new(certPath);
        using CertificateFileMonitor monitor = certificateFile.Monitor(Logger);
        Assert.Equal(originalCert.Thumbprint, monitor.Current.Thumbprint);

        // Create the expected final certificate
        using RSA finalKey = RSA.Create();
        using X509Certificate2 finalCert = finalKey.CreateSelfSignedCertificate();

        using CancellationTokenSource tokenSource = new(TimeSpan.FromMinutes(2));
        Task[] consumers = Enumerable
            .Repeat(monitor, 3)
            .Select(m => m.WaitForThumbprintAsync(finalCert.Thumbprint, TimeSpan.FromMilliseconds(500), tokenSource.Token))
            .ToArray();

        // Continue to edit the certificate multiple times
        for (int i = 0; i < 5; i++)
        {
            // Alternate errors and valid certificates
            if (i % 2 == 0)
            {
                using RSA newKey = RSA.Create();
                using X509Certificate2 newCert = newKey.CreateSelfSignedCertificate();
                await File.WriteAllBytesAsync(certPath, newCert.Export(X509ContentType.Pkcs12), tokenSource.Token);
            }
            else
            {
                await File.WriteAllTextAsync(certPath, "Invalid", tokenSource.Token);
            }

            await Task.Delay(TimeSpan.FromSeconds(10), tokenSource.Token); // Wait enough time for the changes to be polled
        }

        // Write the final cert and await its ingestion
        await File.WriteAllBytesAsync(certPath, finalCert.Export(X509ContentType.Pkcs12), tokenSource.Token);

        // Assert that the certificate is eventually correct
        await Task.WhenAll(consumers);
    }
}
