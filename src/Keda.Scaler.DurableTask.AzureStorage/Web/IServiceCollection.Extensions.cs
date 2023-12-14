// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Keda.Scaler.DurableTask.AzureStorage.Accounts;
using Keda.Scaler.DurableTask.AzureStorage.TaskHub;
using Microsoft.Extensions.DependencyInjection;

namespace Keda.Scaler.DurableTask.AzureStorage.Web;

internal static class IServiceCollectionExtensions
{
    public static IServiceCollection AddDurableTaskScaler(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services
            .AddSingleton<IStorageAccountClientFactory<BlobServiceClient>, BlobServiceClientFactory>()
            .AddSingleton<IStorageAccountClientFactory<QueueServiceClient>, QueueServiceClientFactory>()
            .AddSingleton<IOrchestrationAllocator, OptimalOrchestrationAllocator>()
            .AddScoped<AzureStorageTaskHubClient>();
    }
}
