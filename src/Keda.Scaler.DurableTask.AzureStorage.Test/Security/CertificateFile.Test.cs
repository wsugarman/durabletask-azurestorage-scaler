// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Keda.Scaler.DurableTask.AzureStorage.Security;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Security;

[TestClass]
public sealed class CertificateFileTest
{
    private string _tempPath = "";

    [TestInitialize]
    public void TestInitialize()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _ = Directory.CreateDirectory(_tempPath);
    }

    [TestCleanup]
    public void TestCleanup()
        => Directory.Delete(_tempPath, true);

    [TestMethod]
    public void Reload()
    {
        string certPath = Path.Combine(_tempPath, "tls.crt");

        using ManualResetEventSlim resetEvent = new();

        // Initialize the certificate
        using RSA key1 = RSA.Create();
        using X509Certificate2 cert1 = CreateCertificate(key1);
        WriteCertificate(cert1, certPath);

        using CertificateFile certFile = CertificateFile.From(certPath);
        using IDisposable receipt = ChangeToken.OnChange(certFile.Watch, resetEvent.Set);
        Assert.AreEqual(cert1.Thumbprint, certFile.Current.Thumbprint);
        Assert.IsFalse(resetEvent.IsSet);

        // Overwrite the certificate
        using RSA key2 = RSA.Create();
        using X509Certificate2 cert2 = CreateCertificate(key2);
        WriteCertificate(cert2, certPath + ".new");
        File.Move(certPath + ".new", certPath, overwrite: true);

        Assert.IsTrue(resetEvent.Wait(TimeSpan.FromSeconds(10)));
        Assert.AreEqual(cert2.Thumbprint, certFile.Current.Thumbprint);
    }

    [TestMethod]
    public void ReloadPem()
    {
        string certPath = Path.Combine(_tempPath, "tls.crt");
        string keyPath = Path.Combine(_tempPath, "tls.key");

        using ManualResetEventSlim resetEvent = new();

        // Initialize the certificate
        using RSA key1 = RSA.Create();
        using X509Certificate2 cert1 = CreateCertificate(key1);
        WriteCertificate(cert1, key1, certPath, keyPath);

        using CertificateFile certFile = CertificateFile.FromPemFile(certPath, keyPath);
        using IDisposable receipt = ChangeToken.OnChange(certFile.Watch, resetEvent.Set);
        Assert.AreEqual(cert1.Thumbprint, certFile.Current.Thumbprint);
        Assert.IsFalse(resetEvent.IsSet);

        // Overwrite the certificate
        using RSA key2 = RSA.Create();
        using X509Certificate2 cert2 = CreateCertificate(key2);
        WriteCertificate(cert2, key2, certPath + ".new", keyPath + ".new");
        File.Move(keyPath + ".new", keyPath, overwrite: true);
        File.Move(certPath + ".new", certPath, overwrite: true);

        Assert.IsTrue(resetEvent.Wait(TimeSpan.FromSeconds(10)));
        Assert.AreEqual(cert2.Thumbprint, certFile.Current.Thumbprint);
    }

    [TestMethod]
    public void ReloadCombinedPem()
    {
        string certPath = Path.Combine(_tempPath, "tls.crt");

        using ManualResetEventSlim resetEvent = new();

        // Initialize the certificate
        using RSA key1 = RSA.Create();
        using X509Certificate2 cert1 = CreateCertificate(key1);
        WriteCombinedCertificate(cert1, key1, certPath);

        using CertificateFile certFile = CertificateFile.FromPemFile(certPath);
        using IDisposable receipt = ChangeToken.OnChange(certFile.Watch, resetEvent.Set);
        Assert.AreEqual(cert1.Thumbprint, certFile.Current.Thumbprint);
        Assert.IsFalse(resetEvent.IsSet);

        // Overwrite the certificate
        using RSA key2 = RSA.Create();
        using X509Certificate2 cert2 = CreateCertificate(key2);
        WriteCombinedCertificate(cert2, key2, certPath + ".new");
        File.Move(certPath + ".new", certPath, overwrite: true);

        Assert.IsTrue(resetEvent.Wait(TimeSpan.FromSeconds(10)));
        Assert.AreEqual(cert2.Thumbprint, certFile.Current.Thumbprint);
    }

    private static X509Certificate2 CreateCertificate(RSA key)
    {
        CertificateRequest certRequest = new("cn=unittest", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return certRequest.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddHours(1));
    }

    private static void WriteCertificate(X509Certificate2 certificate, string path)
    {
        // Export the certificate
        using FileStream certStream = File.Create(path);
        using StreamWriter certWriter = new(certStream);
        certWriter.WriteBase64Cert(certificate);
    }

    private static void WriteCertificate(X509Certificate2 certificate, RSA key, string certPath, string keyPath)
    {
        // Export the key
        using FileStream keyStream = File.Create(keyPath);
        using StreamWriter keyWriter = new(keyStream);
        keyWriter.WritePrivateKey(key);

        // Export the certificate
        WriteCertificate(certificate, certPath);
    }

    private static void WriteCombinedCertificate(X509Certificate2 certificate, RSA key, string path)
    {
        using FileStream stream = File.Create(path);
        using StreamWriter writer = new(stream);
        writer.WriteBase64Cert(certificate);
        writer.WritePrivateKey(key);
    }
}
