// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Collections.Generic;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;

namespace Keda.Scaler.DurableTask.AzureStorage.Extensions
{
    internal static class MapFieldExtensions
    {
        public static IConfiguration ToConfiguration(this MapField<string, string> mapField)
            => new ConfigurationRoot(new List<IConfigurationProvider>
            {
                new MemoryConfigurationProvider(new MemoryConfigurationSource { InitialData = mapField })
            });
    }
}
