// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using k8s;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Keda.Scaler.DurableTask.AzureStorage.Common;
using Keda.Scaler.DurableTask.AzureStorage.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

internal static class IServiceCollectionExtensions
{
    public static IServiceCollection AddScaler(this IServiceCollection services)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        services.TryAddScoped<IProcessEnvironment>(p => new EnvironmentCache(ProcessEnvironment.Current));
        services.TryAddSingleton<ITokenCredentialFactory, TokenCredentialFactory>();
        services.TryAddScoped<IDurableTaskAzureStorageScaler, DurableTaskAzureStorageScaler>();

        return services;
    }

    [ExcludeFromCodeCoverage]
    public static IServiceCollection AddKubernetesClient(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        if (configuration is null)
            throw new ArgumentNullException(nameof(configuration));

        if (environment is null)
            throw new ArgumentNullException(nameof(environment));

        if (environment.IsDevelopment())
        {
            services
                .AddOptions<KubernetesClientConfiguration>()
                .Bind(configuration.GetSection("Kubernetes"));

            services.TryAddSingleton<IKubernetes>(p => new Kubernetes(p.GetRequiredService<IOptions<KubernetesClientConfiguration>>().Value));
        }
        else
        {
            services.TryAddSingleton<IKubernetes>(p => new Kubernetes(KubernetesClientConfiguration.InClusterConfig()));
        }

        return services;
    }
}
