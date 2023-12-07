// Copyright © William Sugarman.
// Licensed under the MIT License.

using Google.Protobuf.Collections;
using Microsoft.Extensions.Configuration;

namespace Keda.Scaler.DurableTask.AzureStorage.Protobuf;

internal static class MapFieldExtensions
{
    public static IConfiguration ToConfiguration(this MapField<string, string> mapField)
        => new MapFieldConfiguration(mapField);
}
