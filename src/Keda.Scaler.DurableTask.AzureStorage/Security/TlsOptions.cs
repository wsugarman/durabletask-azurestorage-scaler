// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

internal sealed class TlsOptions
{
    public const string DefaultSectionName = "Security";

    public bool MutualTls { get; set; }

    public string? CertificatePath { get; set; }

    public string? KeyPath { get; set; }

    public string? Password { get; set; }

    public X509Certificate2? ReadCertificate()
    {
        if (CertificatePath is null)
            return null;

        if (Password is null)
            return X509Certificate2.CreateFromPemFile(CertificatePath, KeyPath);

        return X509Certificate2.CreateFromEncryptedPemFile(CertificatePath, Password, KeyPath);
    }
}
