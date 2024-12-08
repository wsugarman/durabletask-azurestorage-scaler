// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.Metadata;

internal class ConfigureScalerOptions(IScalerMetadataAccessor metadataAccessor) : IConfigureOptions<ScalerOptions>
{
    private readonly IScalerMetadataAccessor _metadataAccessor = metadataAccessor ?? throw new ArgumentNullException(nameof(metadataAccessor));

    public void Configure(ScalerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        IReadOnlyDictionary<string, string?> metadata = _metadataAccessor.ScalerMetadata ?? throw new InvalidOperationException(SR.ScalerMetadataNotFound);
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(metadata)
            .Build();

        config.Bind(options);
    }
}
