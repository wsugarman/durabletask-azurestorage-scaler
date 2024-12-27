// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

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
        if (!configuration.IsTlsEnforced())
            return false;

        ClientCertificateValidationOptions options = configuration.GetCertificateValidationOptions();
        return options.Enable && options.CertificateAuthority is not null;
    }

    public static bool ValidateClientCertificate(this IConfiguration configuration)
        => configuration.IsTlsEnforced() && configuration.GetCertificateValidationOptions().Enable;

    private static ClientCertificateValidationOptions GetCertificateValidationOptions(this IConfiguration configuration)
    {
        ClientCertificateValidationOptions options = new();
        configuration.GetSection(ClientCertificateValidationOptions.DefaultKey).Bind(options);
        ValidateOptionsResult result = ValidateClientCertificateValidationOptions.Instance.Validate(Options.DefaultName, options);
        if (result.Failed)
            throw new OptionsValidationException(Options.DefaultName, typeof(ClientCertificateValidationOptions), result.Failures);

        return options;
    }
}
