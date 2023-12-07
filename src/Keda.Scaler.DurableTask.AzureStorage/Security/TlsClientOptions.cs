// Copyright © William Sugarman.
// Licensed under the MIT License.

using Keda.Scaler.DurableTask.AzureStorage.DataAnnotations;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

internal class TlsClientOptions
{
    public const string DefaultKey = "Security:Transport:Client";

    public const string DefaultAuthenticationKey = DefaultKey + ":" + "Authentication";

    public const string DefaultCachingKey = DefaultAuthenticationKey + ":" + "Caching";

    [FileExists]
    public string? CaCertificatePath { get; set; }

    public bool ValidateCertificate { get; set; } = true;
}
