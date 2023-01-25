// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

[ExcludeFromCodeCoverage]
internal sealed class TlsOptions
{
    public const string DefaultSectionName = "Security";

    public bool MutualTls { get; set; }

    public string? CertificatePath { get; set; }

    public string? KeyPath { get; set; }
}
