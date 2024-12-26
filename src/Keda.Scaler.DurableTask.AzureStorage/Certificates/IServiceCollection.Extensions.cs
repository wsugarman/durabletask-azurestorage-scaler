// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.Certificates;

internal static class IServiceCollectionExtensions
{
    public static IServiceCollection AddTlsSupport(this IServiceCollection services, string policyName, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        ArgumentNullException.ThrowIfNull(configuration);

        _ = services
            .AddOptions<ClientCertificateValidationOptions>()
            .BindConfiguration(ClientCertificateValidationOptions.DefaultKey);

        _ = services
            .AddOptions<CertificateAuthenticationOptions>(CertificateAuthenticationDefaults.AuthenticationScheme)
            .Configure<IOptions<ClientCertificateValidationOptions>>((dest, src) => dest.RevocationMode = src.Value.RevocationMode);

        _ = services
            .AddOptions<CertificateValidationCacheOptions>()
            .BindConfiguration(ClientCertificateValidationOptions.DefaultCachingKey);

        if (configuration.ValidateClientCertificate())
        {
            _ = services
            .AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
            .AddCertificate()
            .AddCertificateCache();

            _ = services
                .AddAuthorization(o => o
                    .AddPolicy(policyName, b => b
                        .AddAuthenticationSchemes(CertificateAuthenticationDefaults.AuthenticationScheme)
                        .RequireAuthenticatedUser()));

            if (configuration.UseCustomClientCa())
            {
                _ = services
                    .AddSingleton<ConfigureCustomTrustStore>()
                    .AddSingleton<IConfigureOptions<CertificateAuthenticationOptions>>(p => p.GetRequiredService<ConfigureCustomTrustStore>())
                    .AddSingleton<IOptionsChangeTokenSource<CertificateAuthenticationOptions>>(p => p.GetRequiredService<ConfigureCustomTrustStore>());
            }
        }

        return services;
    }
}
