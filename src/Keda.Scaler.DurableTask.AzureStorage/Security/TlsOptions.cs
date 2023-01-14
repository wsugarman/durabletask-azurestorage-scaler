// Copyright © William Sugarman.
// Licensed under the MIT License.

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

internal sealed class TlsOptions
{
    public const string DefaultSectionName = "Security";

    public bool MutualTls { get; set; }

    public string? CertificatePath { get; set; }

    public string? KeyPath { get; set; }
}
