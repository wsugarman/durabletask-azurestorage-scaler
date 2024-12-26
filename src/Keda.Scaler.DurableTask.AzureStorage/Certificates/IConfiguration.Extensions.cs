// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace Keda.Scaler.DurableTask.AzureStorage.Certificates;

internal static class IConfigurationExtensions
{
    public static bool EnforceTls(this IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        Debug.Assert(configuration is not IConfigurationSection);

        // Note: We use the configuration-based Kestrel settings as a proxy for whether
        // TLS is enabled as that is the only way it is configured via Helm
        return configuration.GetSection("Kestrel:Certificates:Default:Path").Exists();
    }

    public static bool UseCustomClientCa(this IConfiguration configuration)
    {
        return configuration.ValidateClientCertificate()
            && configuration.GetSection("Kestrel:Client:Certificate:Validation:CertificateAuthority:Path").Exists();
    }

    public static bool ValidateClientCertificate(this IConfiguration configuration)
    {
        if (configuration.EnforceTls())
        {
            IConfigurationSection section = configuration.GetSection("Kestrel:Client:Certificate:Validation:Enabled");
            return !section.Exists() || (bool.TryParse(section.Value, out bool enabled) && enabled);
        }

        return false;
    }
}
