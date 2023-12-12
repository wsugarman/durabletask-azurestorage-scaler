// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

internal static class IServiceCollectionExtensions
{
    public static IServiceCollection AddTlsSupport(this IServiceCollection services, string policyName, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        ArgumentNullException.ThrowIfNull(configuration);

        _ = services
            .AddOptions<CertificateAuthenticationOptions>(CertificateAuthenticationDefaults.AuthenticationScheme)
            .BindConfiguration(TlsClientOptions.DefaultAuthenticationKey);

        _ = services
            .AddOptions<CertificateValidationCacheOptions>()
            .BindConfiguration(TlsClientOptions.DefaultCachingKey);

        _ = services
            .AddOptions<TlsClientOptions>()
            .BindConfiguration(TlsClientOptions.DefaultKey)
            .ValidateDataAnnotations();

        _ = services
            .AddOptions<TlsServerOptions>()
            .BindConfiguration(TlsServerOptions.DefaultKey)
            .ValidateDataAnnotations();

        if (configuration.EnforceMutualTls())
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
        }

        return services
            .AddSingleton<TlsConfigure>()
            .AddSingleton<IConfigureOptions<CertificateAuthenticationOptions>>(p => p.GetRequiredService<TlsConfigure>())
            .AddSingleton<IOptionsChangeTokenSource<CertificateAuthenticationOptions>>(p => p.GetRequiredService<TlsConfigure>());
    }
}
