// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;

namespace Keda.Scaler.DurableTask.AzureStorage.Metadata;

// This class is based on HttpContextAccessor
internal sealed class ScalerMetadataAccessor : IScalerMetadataAccessor
{
    private static readonly AsyncLocal<ScalerMetadataHolder> CurrentMetadata = new();

    public IReadOnlyDictionary<string, string?>? ScalerMetadata
    {
        get => CurrentMetadata.Value?.Metadata;
        set
        {
            // Clear current metadata trapped in the AsyncLocals
            _ = CurrentMetadata.Value?.Metadata = null;

            if (value != null)
            {
                // Use an object indirection to hold the HttpContext in the AsyncLocal,
                // so it can be cleared in all ExecutionContexts when its cleared.
                CurrentMetadata.Value = new ScalerMetadataHolder { Metadata = value };
            }
        }
    }

    private sealed class ScalerMetadataHolder
    {
        public IReadOnlyDictionary<string, string?>? Metadata;
    }
}
