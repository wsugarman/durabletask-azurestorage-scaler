// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

internal static class IServiceCollectionExtensions
{
    public static IServiceCollection AddTlsSupport(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _ = services
            .AddOptions<CertificateAuthenticationOptions>(CertificateAuthenticationDefaults.AuthenticationScheme)
            .BindConfiguration(TlsClientOptions.DefaultAuthenticationKey);

        _ = services
            .AddOptions<CertificateValidationCacheOptions>()
            .BindConfiguration(TlsClientOptions.DefaultCachingKey);

        _ = services
            .AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
            .AddCertificate()
            .AddCertificateCache();

        _ = services
            .AddOptions<TlsClientOptions>()
            .BindConfiguration(TlsClientOptions.DefaultKey)
            .ValidateDataAnnotations();

        _ = services
            .AddOptions<TlsServerOptions>()
            .BindConfiguration(TlsServerOptions.DefaultKey)
            .ValidateDataAnnotations();

        return services
            .AddSingleton<TlsConfigure>()
            .AddSingleton<IConfigureOptions<CertificateAuthenticationOptions>>(p => p.GetRequiredService<TlsConfigure>())
            .AddSingleton<IOptionsChangeTokenSource<CertificateAuthenticationOptions>>(p => p.GetRequiredService<TlsConfigure>());
    }
}
