// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.Configuration;
using Microsoft.Extensions.Configuration;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

internal static class IConfigurationExtensions
{
    public static bool EnforceMutualTls(this IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        TlsServerOptions serverOptions = configuration
            .GetSection(TlsServerOptions.DefaultKey)
            .GetOrCreate<TlsServerOptions>();

        TlsClientOptions tlsClientOptions = configuration
            .GetSection(TlsClientOptions.DefaultKey)
            .GetOrCreate<TlsClientOptions>();

        return serverOptions.EnforceTls && tlsClientOptions.ValidateCertificate;
    }
}
