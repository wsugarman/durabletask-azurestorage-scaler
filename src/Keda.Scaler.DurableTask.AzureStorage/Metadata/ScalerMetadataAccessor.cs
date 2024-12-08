// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;

namespace Keda.Scaler.DurableTask.AzureStorage.Metadata;

internal sealed class ScalerMetadataAccessor : IScalerMetadataAccessor
{
    private IReadOnlyDictionary<string, string?>? _scalerMetadata;

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string?>? ScalerMetadata
    {
        get => Volatile.Read(ref _scalerMetadata);
        set => Volatile.Write(ref _scalerMetadata, value);
    }
}
