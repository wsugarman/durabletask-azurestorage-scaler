// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Threading;

namespace Keda.Scaler.DurableTask.AzureStorage;

[DebuggerDisplay("ScalerMetadata = {ScalerMetadata}")]
internal sealed class ScalerMetadataAccessor : IScalerMetadataAccessor
{
    private ScalerMetadata? _scalerContext;

    /// <inheritdoc/>
    public ScalerMetadata? ScalerMetadata
    {
        get => Volatile.Read(ref _scalerContext);
        set => Volatile.Write(ref _scalerContext, value);
    }
}
