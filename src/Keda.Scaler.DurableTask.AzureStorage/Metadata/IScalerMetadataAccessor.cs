// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Keda.Scaler.DurableTask.AzureStorage.Metadata;

/// <summary>
/// Provides access to the current requests metadata, if any is available.
/// </summary>
public interface IScalerMetadataAccessor
{
    /// <summary>
    /// Gets or sets the current scaler metadata.
    /// </summary>
    /// <value>
    /// The current metadata if available; otherwise, <see langword="null"/>.
    /// </value>
    IReadOnlyDictionary<string, string?>? ScalerMetadata { get; set; }
}
