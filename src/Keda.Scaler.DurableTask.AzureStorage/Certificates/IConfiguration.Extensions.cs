// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace Keda.Scaler.DurableTask.AzureStorage.Certificates;

internal static class IConfigurationExtensions
{
    public static bool IsTlsEnforced(this IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        Debug.Assert(configuration is not IConfigurationSection);

        // Note: We use the configuration-based Kestrel settings as a proxy for whether
        // TLS is enabled as that is the only way it is configured via Helm
        return !string.IsNullOrWhiteSpace(configuration.GetSection("Kestrel:Certificates:Default:Path").Value);
    }

    public static bool UseCustomClientCa(this IConfiguration configuration)
    {
        return configuration.ValidateClientCertificate()
            && !string.IsNullOrWhiteSpace(configuration.GetSection("Kestrel:Client:Certificate:Validation:CertificateAuthority:Path").Value);
    }

    public static bool ValidateClientCertificate(this IConfiguration configuration)
    {
        if (configuration.IsTlsEnforced())
        {
            string? value = configuration.GetSection("Kestrel:Client:Certificate:Validation:Enabled").Value;
            return string.IsNullOrWhiteSpace(value) || (bool.TryParse(value, out bool enabled) && enabled);
        }

        return false;
    }
}
