// Copyright © William Sugarman.
// Licensed under the MIT License.

using Keda.Scaler.DurableTask.AzureStorage.DataAnnotations;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

internal class TlsClientOptions
{
    public const string DefaultKey = "Security:Transport:Client";

    [FileExists]
    public string? CaCertificatePath { get; set; }

    public bool ValidateCertificate { get; set; } = true;
}
