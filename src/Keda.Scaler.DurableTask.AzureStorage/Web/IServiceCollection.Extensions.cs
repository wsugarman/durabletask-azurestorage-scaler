// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Keda.Scaler.DurableTask.AzureStorage.Accounts;
using Keda.Scaler.DurableTask.AzureStorage.Common;
using Keda.Scaler.DurableTask.AzureStorage.TaskHub;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
}
