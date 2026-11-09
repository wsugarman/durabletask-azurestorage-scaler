// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.Metadata;

internal class ConfigureScalerOptions(ScalerMetadata metadata) : IConfigureOptions<ScalerOptions>
{
    private readonly ScalerMetadata _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));

    public void Configure(ScalerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        IReadOnlyDictionary<string, string?> metadata = _metadata.Values ?? throw new InvalidOperationException(SR.ScalerMetadataNotFound);
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(metadata)
            .Build();

        config.Bind(options);
    }
}
