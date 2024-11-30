// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Keda.Scaler.DurableTask.AzureStorage.Accounts;
using Keda.Scaler.DurableTask.AzureStorage.Blobs;
using Keda.Scaler.DurableTask.AzureStorage.Clients;
using Keda.Scaler.DurableTask.AzureStorage.Json;
using Microsoft.Extensions.Logging;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHub;

internal class LegacyAzureStorageTaskHubClient(
    IStorageAccountClientFactory<BlobServiceClient> blobServiceClientFactory,
    IStorageAccountClientFactory<QueueServiceClient> queueServiceClientFactory,
    ILoggerFactory loggerFactory) : AzureStorageTaskHubClient(queueServiceClientFactory, loggerFactory)
{
    private readonly IStorageAccountClientFactory<BlobServiceClient> _blobServiceClientFactory = blobServiceClientFactory ?? throw new ArgumentNullException(nameof(blobServiceClientFactory));

    protected override async ValueTask<int> GetPartitionCountAsync(AzureStorageAccountOptions accountInfo, string taskHub, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountInfo);
        ArgumentException.ThrowIfNullOrWhiteSpace(taskHub);

        // Fetch metadata about the Task Hub
        BlobClient client = _blobServiceClientFactory
            .GetServiceClient(accountInfo)
            .GetBlobContainerClient(LeasesContainer.GetName(taskHub))
            .GetBlobClient(LeasesContainer.TaskHubBlobName);

        BlobDownloadResult result = await client.DownloadContentAsync(cancellationToken).ConfigureAwait(false);
        AzureStorageTaskHubInfo? info = JsonSerializer.Deserialize(
            result.Content.ToMemory().Span,
            SourceGenerationContext.Default.AzureStorageTaskHubInfo);

        if (info is null)
        {
            Logger.CouldNotFindTaskHub(taskHub, client.Name, client.BlobContainerName);
            return 0;
        }

        Logger.FoundTaskHub(info.TaskHubName, info.PartitionCount, info.CreatedAt);
        return info.PartitionCount;
    }
}
