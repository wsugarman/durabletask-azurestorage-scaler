// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Diagnostics.CodeAnalysis;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Security;

internal static class CertificateExtensions
{
    [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Used for generation of unit test certificate serial number.")]
    public static X509Certificate2 CreateCertificate(this RSA key, X509Certificate2 issuer, string testName)
    {
        Random rng = new(testName.GetHashCode(StringComparison.Ordinal));
        CertificateRequest certRequest = new("cn=unittest", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        byte[] serialNumber = new byte[20];
        rng.NextBytes(serialNumber);
        return certRequest.Create(issuer, DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddHours(1), serialNumber);
    }

    public static X509Certificate2 CreateSelfSignedCertificate(this RSA key)
    {
        CertificateRequest certRequest = new("cn=unittest", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
        {
            CertificateExtensions =
            {
                { new X509BasicConstraintsExtension(true, true, 10, false) },
                { new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, false) },
            }
        };

        return certRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddHours(-2), DateTimeOffset.UtcNow.AddHours(2));
    }

    public static string? ExportCertificatePem(this X509Certificate2 certificate, RSA key)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        ArgumentNullException.ThrowIfNull(key);

        return certificate.ExportCertificatePem() + Environment.NewLine + key.ExportRSAPrivateKeyPem();
    }
}
