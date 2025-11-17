// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Keda.Scaler.DurableTask.AzureStorage.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHubs;

internal sealed partial class BlobPartitionManager(BlobServiceClient blobServiceClient, IOptionsSnapshot<TaskHubOptions> options, ILoggerFactory loggerFactory) : ITaskHubPartitionManager
{
    private readonly BlobServiceClient _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
    private readonly TaskHubOptions _options = options?.Get(default) ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger _logger = loggerFactory?.CreateLogger(LogCategories.Default) ?? throw new ArgumentNullException(nameof(loggerFactory));

    public async ValueTask<IReadOnlyList<string>> GetPartitionsAsync(CancellationToken cancellationToken = default)
    {
        // Fetch the blob
        BlobClient client = _blobServiceClient
            .GetBlobContainerClient(LeasesContainer.GetName(_options.TaskHubName))
            .GetBlobClient(LeasesContainer.TaskHubBlobName);

        AzureStorageTaskHubInfo? info;
        try
        {
            BlobDownloadResult result = await client.DownloadContentAsync(cancellationToken);

            // Parse the information about the task hub
            info = JsonSerializer.Deserialize(
                result.Content.ToMemory().Span,
                SourceGenerationContext.Default.AzureStorageTaskHubInfo);
        }
        catch (RequestFailedException rfe) when (rfe.Status is (int)HttpStatusCode.NotFound)
        {
            info = null;
        }

        if (info is null)
        {
            LogMissingTaskHubBlob(_logger, _options.TaskHubName, LeasesContainer.TaskHubBlobName, client.BlobContainerName);
            return [];
        }
        else
        {
            LogTaskHubBlob(_logger, info.TaskHubName, info.PartitionCount, info.CreatedAt, LeasesContainer.TaskHubBlobName);
            return Enumerable
                .Repeat(info.TaskHubName, info.PartitionCount)
                .Select((t, i) => ControlQueue.GetName(info.TaskHubName, i))
                .ToList();
        }
    }

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Found Task Hub '{TaskHubName}' with {Partitions} partitions created at {CreatedTime:O} in blob {TaskHubBlobName}.")]
    private static partial void LogTaskHubBlob(ILogger logger, string taskHubName, int partitions, DateTimeOffset createdTime, string taskHubBlobName);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Cannot find Task Hub '{TaskHubName}' metadata blob '{TaskHubBlobName}' in container '{LeaseContainerName}'.")]
    private static partial void LogMissingTaskHubBlob(ILogger logger, string taskHubName, string taskHubBlobName, string leaseContainerName);

    internal sealed class AzureStorageTaskHubInfo
    {
        [Required]
        public DateTimeOffset CreatedAt { get; init; }

        [Range(1, 15)]
        public int PartitionCount { get; init; }

        [Required]
        public string TaskHubName { get; init; } = default!;
    }
}
