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
using Xunit;
using Xunit.Abstractions;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Security;

public sealed class CertificateFileMonitorIntegrationTest(ITestOutputHelper outputHelper) : FileSystemTest(outputHelper)
{
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

            using CancellationTokenSource tokenSource = new();
            Task consumer = monitor.WaitForThumbprintAsync(cert.Thumbprint, TimeSpan.FromMilliseconds(200), tokenSource.Token);
            tokenSource.CancelAfter(TimeSpan.FromMinutes(1));
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

        using CancellationTokenSource tokenSource = new();
        Task[] consumers = Enumerable
            .Repeat(monitor, 3)
            .Select(m => m.WaitForThumbprintAsync(finalCert.Thumbprint, TimeSpan.FromMilliseconds(500), tokenSource.Token))
            .ToArray();

        // Continue to edit the certificate multiple times
        for (int i = 0; i < 5; i++)
        {
            using RSA newKey = RSA.Create();
            using X509Certificate2 newCert = newKey.CreateSelfSignedCertificate();
            await File.WriteAllBytesAsync(certPath, newCert.Export(X509ContentType.Pkcs12));
            await Task.Delay(TimeSpan.FromSeconds(10));
        }

        // Write the final cert and await its ingestion
        await File.WriteAllBytesAsync(certPath, finalCert.Export(X509ContentType.Pkcs12));

        // Assert that the certificate is eventually correct
        tokenSource.CancelAfter(TimeSpan.FromMinutes(2));
        await Task.WhenAll(consumers);
    }
}
