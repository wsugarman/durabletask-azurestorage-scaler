// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Keda.Scaler.DurableTask.AzureStorage.Accounts;
using Keda.Scaler.DurableTask.AzureStorage.Blobs;
using Microsoft.Extensions.Logging;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHub;

/// <summary>
/// Represents a browser over one or more Durable Task Hubs that use the Azure Storage backend provider.
/// </summary>
public class AzureStorageTaskHubBrowser
{
    private readonly IStorageAccountClientFactory<BlobServiceClient> _blobServiceClientFactory;
    private readonly IStorageAccountClientFactory<QueueServiceClient> _queueServiceClientFactory;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureStorageTaskHubBrowser"/> class.
    /// </summary>
    /// <param name="blobServiceClientFactory">A factory for creating Azure Blob Storage service clients.</param>
    /// <param name="queueServiceClientFactory">A factory for creating Azure Queue service clients.</param>
    /// <param name="loggerFactory">A factory for creating loggers.</param>
    /// <exception cref="ArgumentNullException"><paramref name="loggerFactory"/> is <see langword="null"/>.</exception>
    public AzureStorageTaskHubBrowser(
        IStorageAccountClientFactory<BlobServiceClient> blobServiceClientFactory,
        IStorageAccountClientFactory<QueueServiceClient> queueServiceClientFactory,
        ILoggerFactory loggerFactory)
    {
        _blobServiceClientFactory = blobServiceClientFactory ?? throw new ArgumentNullException(nameof(blobServiceClientFactory));
        _queueServiceClientFactory = queueServiceClientFactory ?? throw new ArgumentNullException(nameof(queueServiceClientFactory));
        _logger = loggerFactory?.CreateLogger(Diagnostics.LoggerCategory) ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <summary>
    /// Asynchronously attempts to retrieve an <see cref="ITaskHubQueueMonitor"/> for the Task Hub with the given name.
    /// </summary>
    /// <param name="accountInfo">The account information for the Azure Storage account.</param>
    /// <param name="taskHub">The name of the desired Task Hub.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A value task that represents the asynchronous operation. The value of the type parameter
    /// of the value task contains the monitor for the given Task Hub.
    /// </returns>
    /// <exception cref="ArgumentException"><paramref name="accountInfo"/> is missing information.</exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="accountInfo"/> or <paramref name="taskHub"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> is canceled.</exception>
    /// <exception cref="RequestFailedException">
    /// A problem occurred connecting to the Storage Account based on <paramref name="accountInfo"/>.
    /// </exception>
    public virtual async ValueTask<ITaskHubQueueMonitor> GetMonitorAsync(AzureStorageAccountInfo accountInfo, string taskHub, CancellationToken cancellationToken = default)
    {
        if (accountInfo is null)
            throw new ArgumentNullException(nameof(accountInfo));

        if (string.IsNullOrWhiteSpace(taskHub))
            throw new ArgumentNullException(nameof(taskHub));

        // Fetch metadata about the Task Hub
        BlobClient client = _blobServiceClientFactory
            .GetServiceClient(accountInfo)
            .GetBlobContainerClient(LeasesContainer.GetName(taskHub))
            .GetBlobClient(LeasesContainer.TaskHubBlobName);

        try
        {
            BlobDownloadResult result = await client.DownloadContentAsync(cancellationToken).ConfigureAwait(false);
            AzureStorageTaskHubInfo info = result.Content.ToObjectFromJson<AzureStorageTaskHubInfo>();

            _logger.FoundTaskHub(info.TaskHubName, info.PartitionCount, info.CreatedAt);
            return new TaskHubQueueMonitor(info, _queueServiceClientFactory.GetServiceClient(accountInfo), _logger);
        }
        catch (RequestFailedException rfe) when (rfe.Status == (int)HttpStatusCode.NotFound)
        {
            _logger.CouldNotFindTaskHub(taskHub, client.Name, client.BlobContainerName);
            return NullTaskHubQueueMonitor.Instance;
        }
    }
}
