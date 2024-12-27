// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Certificates;

internal static class RSAExtensions
{
    public static X509Certificate2 CreateSelfSignedCertificate(this RSA key)
    {
        ArgumentNullException.ThrowIfNull(key);

        CertificateRequest certRequest = new("cn=unittest", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
        {
            CertificateExtensions =
            {
                { new X509BasicConstraintsExtension(
                    certificateAuthority: true,
                    hasPathLengthConstraint: true,
                    pathLengthConstraint: 10,
                    critical: false)
                },
                { new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, false) },
            }
        };

        return certRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddHours(-2), DateTimeOffset.UtcNow.AddHours(2));
    }
}
