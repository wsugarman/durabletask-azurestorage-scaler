// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using k8s;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Keda.Scaler.DurableTask.AzureStorage.Common;
using Keda.Scaler.DurableTask.AzureStorage.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Keda.Scaler.DurableTask.AzureStorage.Extensions
{
    internal static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddScaler(this IServiceCollection services)
        {
            if (services is null)
                throw new ArgumentNullException(nameof(services));

            services.TryAddScoped<IEnvironment>(p => new EnvironmentCache(CurrentEnvironment.Instance));
            services.TryAddSingleton<ITokenCredentialFactory, TokenCredentialFactory>();
            services.TryAddScoped<IDurableTaskAzureStorageScaler, DurableTaskAzureStorageScaler>();

            return services;
        }

        public static IServiceCollection AddKubernetesClient(this IServiceCollection services)
        {
            if (services is null)
                throw new ArgumentNullException(nameof(services));

            services.TryAddSingleton<IKubernetes>(p => new Kubernetes(KubernetesClientConfiguration.InClusterConfig()));

            return services;
        }
    }
}
