// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Configuration;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

internal static class IConfigurationExtensions
{
    public static bool HasClientValidation(this IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return !string.IsNullOrWhiteSpace(configuration[TlsClientOptions.DefaultKey + ':' + nameof(TlsClientOptions.CaCertificatePath)]);
    }
}
