// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Configuration;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

internal static class IConfigurationExtensions
{
    public static bool EnforceMutualTls(this IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        TlsServerOptions tlsServerOptions = new();
        configuration.GetSection(TlsServerOptions.DefaultKey).Bind(tlsServerOptions);

        TlsClientOptions tlsClientOptions = new();
        configuration.GetSection(TlsClientOptions.DefaultKey).Bind(tlsClientOptions);

        return tlsServerOptions.EnforceTls && tlsClientOptions.ValidateCertificate;
    }

    public static bool EnforceTls(this IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        TlsServerOptions tlsServerOptions = new();
        configuration.GetSection(TlsServerOptions.DefaultKey).Bind(tlsServerOptions);

        return tlsServerOptions.EnforceTls;
    }

    public static bool UseCustomClientCa(this IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        TlsClientOptions tlsClientOptions = new();
        configuration.GetSection(TlsClientOptions.DefaultKey).Bind(tlsClientOptions);

        return !string.IsNullOrWhiteSpace(tlsClientOptions.CaCertificatePath);
    }
}
