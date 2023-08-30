// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

[ExcludeFromCodeCoverage]
internal sealed class TlsOptions
{
    public const string DefaultKey = "Security:Transport";

    public string? ClientCaCertificatePath { get; set; }

    public string? ServerCertificatePath { get; set; }

    public string? ServerKeyPath { get; set; }
}
