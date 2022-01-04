// Copyright © William Sugarman.
// Licensed under the MIT License.

using Google.Protobuf.Collections;
using Keda.Scaler.DurableTask.AzureStorage.Protobuf;
using Microsoft.Extensions.Configuration;

namespace Keda.Scaler.DurableTask.AzureStorage.Extensions;

internal static class MapFieldExtensions
{
    public static IConfiguration ToConfiguration(this MapField<string, string> mapField)
        => new MapFieldConfiguration(mapField);
}
