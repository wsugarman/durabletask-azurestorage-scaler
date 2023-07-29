// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

[ExcludeFromCodeCoverage]
internal sealed class TlsClientOptions
{
    public string? CaCertificatePath { get; set; }

    public string? CaKeyPath { get; set; }
}
