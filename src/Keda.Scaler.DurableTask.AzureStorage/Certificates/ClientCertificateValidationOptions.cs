// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;

namespace Keda.Scaler.DurableTask.AzureStorage.Certificates;

// Based on https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Configuration.FileExtensions/src/FileConfigurationSource.cs#L14
internal sealed class ClientCertificateValidationOptions
{
    public const string DefaultKey = "Kestrel:Client:Certificate:Validation";
    public const string DefaultCachingKey = $"{DefaultKey}:Caching";

    public bool Enable { get; set; } = true;

    public CaCertificateFileOptions? CertificateAuthority { get; set; }

    public X509RevocationMode RevocationMode { get; set; } = X509RevocationMode.Online;
}
