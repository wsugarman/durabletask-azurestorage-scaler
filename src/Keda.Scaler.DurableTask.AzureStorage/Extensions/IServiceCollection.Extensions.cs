// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Keda.Scaler.DurableTask.AzureStorage.Accounts;
using Keda.Scaler.DurableTask.AzureStorage.Common;
using Keda.Scaler.DurableTask.AzureStorage.Security;
using Keda.Scaler.DurableTask.AzureStorage.TaskHub;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

internal static class IServiceCollectionExtensions
{
    public static IServiceCollection AddDurableTaskScaler(this IServiceCollection services)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        services.TryAddSingleton<IStorageAccountClientFactory<BlobServiceClient>, BlobServiceClientFactory>();
        services.TryAddSingleton<IStorageAccountClientFactory<QueueServiceClient>, QueueServiceClientFactory>();
        services.TryAddSingleton<IOrchestrationAllocator, OptimalOrchestrationAllocator>();
        services.TryAddScoped<IProcessEnvironment>(p => new EnvironmentCache(ProcessEnvironment.Current));
        services.TryAddScoped<AzureStorageTaskHubBrowser>();

        return services;
    }

    public static IServiceCollection AddTlsSupport(this IServiceCollection services, IConfiguration configuration)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        if (configuration is null)
            throw new ArgumentNullException(nameof(configuration));

        services
            .Configure<TlsOptions>(configuration.GetSection(TlsOptions.DefaultKey))
            .TryAddSingleton<TlsConnectionAdapterOptionsConfigure>();

        return services;
    }
}
