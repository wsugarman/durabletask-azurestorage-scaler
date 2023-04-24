// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Configuration;

namespace Keda.Scaler.DurableTask.AzureStorage.Extensions;

internal static class IConfigurationExtensions
{
    public static T GetOrDefault<T>(this IConfiguration configuration)
        where T : new()
    {
        if (configuration is null)
            throw new ArgumentNullException(nameof(configuration));

        T obj = new T();
        configuration.Bind(obj);
        return obj;
    }
}
