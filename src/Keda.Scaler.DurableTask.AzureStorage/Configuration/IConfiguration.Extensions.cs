// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Configuration;

namespace Keda.Scaler.DurableTask.AzureStorage.Configuration;

internal static class IConfigurationExtensions
{
    public static T GetOrDefault<T>(this IConfiguration configuration)
        where T : new()
    {
        ArgumentNullException.ThrowIfNull(configuration);

        T obj = new();
        configuration.Bind(obj);
        return obj;
    }
}
