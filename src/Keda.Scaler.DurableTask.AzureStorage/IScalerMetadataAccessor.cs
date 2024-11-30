// Copyright © William Sugarman.
// Licensed under the MIT License.

namespace Keda.Scaler.DurableTask.AzureStorage;

/// <summary>
/// Provides access to the current <see cref="ScalerMetadata"/>, if one is available.
/// </summary>
public interface IScalerMetadataAccessor
{
    /// <summary>
    /// Gets or sets the current <see cref="ScalerMetadata"/>
    /// </summary>
    /// <value>
    /// The current <see cref="ScalerMetadata"/> if available; otherwise, <see langword="null"/>.
    /// </value>
    ScalerMetadata? ScalerMetadata { get; set; }
}
