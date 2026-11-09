// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Keda.Scaler.DurableTask.AzureStorage.Metadata;

/// <summary>
/// Represents the metadata for a KEDA scaler.
/// </summary>
public sealed class ScalerMetadata
{
    /// <summary>
    /// Gets or sets the user-specific metadata.
    /// </summary>
    /// <value>A series of key-value pairs.</value>
    public IReadOnlyDictionary<string, string?>? Values { get; set; }
}
