// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Keda.Scaler.DurableTask.AzureStorage.Accounts;
using Keda.Scaler.DurableTask.AzureStorage.Common;
using Keda.Scaler.DurableTask.AzureStorage.Security;
using Keda.Scaler.DurableTask.AzureStorage.TaskHub;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.Web;

internal static class IServiceCollectionExtensions
{
    public static IServiceCollection AddDurableTaskScaler(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IStorageAccountClientFactory<BlobServiceClient>, BlobServiceClientFactory>();
        services.TryAddSingleton<IStorageAccountClientFactory<QueueServiceClient>, QueueServiceClientFactory>();
        services.TryAddSingleton<IOrchestrationAllocator, OptimalOrchestrationAllocator>();
        services.TryAddScoped<IProcessEnvironment>(p => new EnvironmentCache(ProcessEnvironment.Current));
        services.TryAddScoped<AzureStorageTaskHubBrowser>();

        return services;
    }

    public static IServiceCollection AddTlsSupport(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        _ = services
            .AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
            .AddCertificate();

        _ = services
            .AddOptions<TlsClientOptions>()
            .BindConfiguration(TlsClientOptions.DefaultKey)
            .ValidateDataAnnotations();

        _ = services
            .AddOptions<TlsServerOptions>()
            .BindConfiguration(TlsServerOptions.DefaultKey)
            .ValidateDataAnnotations();

        services.TryAddSingleton<TlsConfigure>();
        services.TryAddSingleton<IConfigureOptions<CertificateAuthenticationOptions>>(p => p.GetRequiredService<TlsConfigure>());
        services.TryAddSingleton<IOptionsChangeTokenSource<CertificateAuthenticationOptions>>(p => p.GetRequiredService<TlsConfigure>());

        return services;
    }
}
