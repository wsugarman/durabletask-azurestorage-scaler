// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.Clients;

internal static class AzureStorageServiceClientExtensions
{
    public static IServiceCollection AddAzureStorageServiceClients(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services
            .AddSingleton<BlobServiceClientFactory>()
            .AddSingleton<QueueServiceClientFactory>()
            .AddSingleton<TableServiceClientFactory>()
            .AddScoped<IConfigureOptions<AzureStorageAccountOptions>, ConfigureAzureStorageAccountOptions>()
            .AddScoped<IValidateOptions<AzureStorageAccountOptions>, ValidateAzureStorageAccountOptions>()
            .AddScoped(sp => GetBlobServiceClient(sp.GetRequiredService<BlobServiceClientFactory>(), sp.GetRequiredService<IOptionsSnapshot<AzureStorageAccountOptions>>()))
            .AddScoped(sp => GetQueueServiceClient(sp.GetRequiredService<QueueServiceClientFactory>(), sp.GetRequiredService<IOptionsSnapshot<AzureStorageAccountOptions>>()))
            .AddScoped(sp => GetTableServiceClient(sp.GetRequiredService<TableServiceClientFactory>(), sp.GetRequiredService<IOptionsSnapshot<AzureStorageAccountOptions>>()));
    }

    private static BlobServiceClient GetBlobServiceClient(BlobServiceClientFactory factory, IOptionsSnapshot<AzureStorageAccountOptions> options)
        => factory.GetServiceClient(options.Get(default));

    private static QueueServiceClient GetQueueServiceClient(QueueServiceClientFactory factory, IOptionsSnapshot<AzureStorageAccountOptions> options)
        => factory.GetServiceClient(options.Get(default));

    private static TableServiceClient GetTableServiceClient(TableServiceClientFactory factory, IOptionsSnapshot<AzureStorageAccountOptions> options)
        => factory.GetServiceClient(options.Get(default));
}
