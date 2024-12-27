// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Collections.Generic;
using Keda.Scaler.DurableTask.AzureStorage.Metadata;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Metadata;

public class ScalerMetadataAccessorTest
{
    [Fact]
    public void GivenNoMetadata_WhenAccessingMetadata_ThenReturnNull()
        => Assert.Null(new ScalerMetadataAccessor().ScalerMetadata);

    [Fact]
    public void GivenMetadata_WhenAccessingMetadata_ThenReturnMetadata()
    {
        IReadOnlyDictionary<string, string?> metadata = new Dictionary<string, string?> { { "key", "value" } };
        ScalerMetadataAccessor accessor = new() { ScalerMetadata = metadata };
        Assert.Same(metadata, accessor.ScalerMetadata);
    }
}
