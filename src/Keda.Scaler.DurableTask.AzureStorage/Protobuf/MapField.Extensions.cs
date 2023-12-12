// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;

namespace Keda.Scaler.DurableTask.AzureStorage.Protobuf;

internal static class MapFieldExtensions
{
    public static IConfiguration ToConfiguration(this MapField<string, string> mapField)
    {
        ArgumentNullException.ThrowIfNull(mapField);

#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
        return new ConfigurationRoot([new MemoryConfigurationProvider(new MemoryConfigurationSource { InitialData = mapField })]);
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
    }
}
