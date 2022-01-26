// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using EnsureThat;
using Microsoft.Extensions.Logging;

namespace Keda.Scaler.DurableTask.AzureStorage.Monitor;

internal class TaskHubBrowser : ITaskHubBrowser
{
    private const string TaskHubBlobName = "taskhub.json";
    private readonly ILogger<TaskHubBrowser> _logger;
    private readonly BlobServiceClient _client;

    public TaskHubBrowser(BlobServiceClient client, ILogger<TaskHubBrowser> logger)
    {
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        _client = EnsureArg.IsNotNull(client, nameof(client));
    }

    public async ValueTask<TaskHubInfo?> GetAsync(string taskHubName, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotEmptyOrWhiteSpace(taskHubName, nameof(taskHubName));
        BlobContainerClient containerClient = _client.GetBlobContainerClient(GetLeaseContainerName(taskHubName));
        BlobClient blobClient = containerClient.GetBlobClient(TaskHubBlobName);

        try
        {
            Stream blob = await blobClient.OpenReadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            await using (blob.ConfigureAwait(false))
            {
                return await JsonSerializer.DeserializeAsync<TaskHubInfo>(blob, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
        {
            _logger.LogWarning(
                "Cannot find Task Hub lease '{LeaseName}' for Task Hub '{TaskHubName}' in Azure Storage Account '{AccountName}'.",
                containerClient.Name,
                taskHubName,
                _client.AccountName);

            return null;
        }
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "DurableTask framework normalizes to lowercase.")]
    public static string GetLeaseContainerName(string taskHubName)
          => !string.IsNullOrEmpty(taskHubName)
          ? taskHubName.ToLowerInvariant() + "-leases"
          : throw new ArgumentNullException(nameof(taskHubName));
}
