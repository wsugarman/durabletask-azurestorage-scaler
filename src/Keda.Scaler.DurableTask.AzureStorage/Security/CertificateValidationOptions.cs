// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

// This class contains a subset of the members in ClientAuthenticationOptions
internal class CertificateValidationOptions
{
    public const string DefaultKey = TlsClientOptions.DefaultKey + ":" + "Authentication";

    public const string DefaultCachingKey = DefaultKey + ":" + "Caching";

    public X509RevocationMode RevocationMode { get; set; } = X509RevocationMode.Online;
}
