// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Keda.Scaler.DurableTask.AzureStorage.Security;
using System.Threading;
using Microsoft.Extensions.Primitives;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Security;

[TestClass]
public sealed class CertificateFileTest : IDisposable
{
    private readonly string _tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public CertificateFileTest()
        => Directory.CreateDirectory(_tempPath);

    [TestMethod]
    public void Reload()
    {
        string certPath = Path.Combine(_tempPath, "cert.pem");
        string keyPath = Path.Combine(_tempPath, "key.pem");

        using ManualResetEventSlim resetEvent = new();

        // Initialize the certificate
        using RSA key1 = RSA.Create();
        using X509Certificate2 cert1 = CreateCertificate(key1);
        WriteCertificate(cert1, key1, certPath, keyPath);

        using CertificateFile certFile = new(certPath, keyPath);
        using IDisposable receipt = ChangeToken.OnChange(certFile.Watch, resetEvent.Set);
        Assert.AreEqual(cert1.Thumbprint, certFile.Current.Thumbprint);
        Assert.IsFalse(resetEvent.IsSet);

        // Overwrite the certificate
        using RSA key2 = RSA.Create();
        using X509Certificate2 cert2 = CreateCertificate(key2);
        WriteCertificate(cert2, key2, certPath, keyPath);

        Assert.IsTrue(resetEvent.Wait(TimeSpan.FromSeconds(10)));
        Assert.AreEqual(cert2.Thumbprint, certFile.Current.Thumbprint);
    }

    public void Dispose()
        => Directory.Delete(_tempPath, true);

    private static X509Certificate2 CreateCertificate(RSA key)
    {
        CertificateRequest certRequest = new("cn=unit-test", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return certRequest.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddHours(1));
    }

    private static void WriteCertificate(X509Certificate2 certificate, RSA key, string certPath, string keyPath)
    {
        // Export the key
        using FileStream keyStream = File.Create(keyPath);
        using StreamWriter keyWriter = new(keyStream);
        keyWriter.WriteLine("-----BEGIN RSA PRIVATE KEY-----");
        keyWriter.WriteLine(Convert.ToBase64String(key.ExportRSAPrivateKey(), Base64FormattingOptions.InsertLineBreaks));
        keyWriter.WriteLine("-----END RSA PRIVATE KEY-----");

        // Export the certificate
        using FileStream certStream = File.Create(certPath);
        using StreamWriter certWriter = new(certStream);
        certWriter.WriteLine("-----BEGIN CERTIFICATE-----");
        certWriter.WriteLine(Convert.ToBase64String(certificate.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks));
        certWriter.WriteLine("-----END CERTIFICATE-----");
    }
}
