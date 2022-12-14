// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Keda.Scaler.DurableTask.AzureStorage.Account;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Keda.Scaler.DurableTask.AzureStorage.Extensions;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHub;

/// <summary>
/// Represents a browser over one or more Durable Task Hubs in an Azure Storage account.
/// </summary>
public class AzureStorageTaskHubBrowser
{
    /// <summary>
    /// Asynchronously attempts to retrieve an <see cref="ITaskHubMonitor"/> for the Task Hub
    /// with the given <paramref name="name"/>.
    /// </summary>
    /// <param name="accountInfo">The account information for the Azure Storage account.</param>
    /// <param name="taskHub">The name of the desired Task Hub.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A value task that represents the asynchronous operation. The value of the type parameter
    /// of the value task contains the monitor for the given Task Hub if found; otherwise, <see langword="null"/>.
    /// </returns>
    /// <exception cref="ArgumentException"><paramref name="accountInfo"/> is missing information.</exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="accountInfo"/> or <paramref name="taskHub"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> is canceled.</exception>
    /// <exception cref="RequestFailedException">
    /// A problem occurred connecting to the Storage Account based on <paramref name="accountInfo"/>.
    /// </exception>
    public virtual ValueTask<ITaskHubMonitor> GetMonitorAsync(AzureStorageAccountInfo accountInfo, string taskHub, CancellationToken cancellationToken = default)
    {
        if (accountInfo is null)
            throw new ArgumentNullException(nameof(accountInfo));

        if (string.IsNullOrWhiteSpace(taskHub))
            throw new ArgumentNullException(nameof(taskHub));

        BlobServiceClient blobServiceClient;
        QueueServiceClient queueServiceClient;
        TableServiceClient tableServiceClient;

        if (string.IsNullOrWhiteSpace(accountInfo.ConnectionString))
        {
            if (string.IsNullOrWhiteSpace(accountInfo.AccountName))
                throw new ArgumentException(SR.Format(SR.MissingMemberFormat, nameof(accountInfo.AccountName)), nameof(accountInfo));

            if (accountInfo.CloudEnvironment == CloudEnvironment.Unknown)
                throw new ArgumentException(SR.Format(SR.MissingMemberFormat, nameof(accountInfo.CloudEnvironment)), nameof(accountInfo));

            CloudEndpoints endpoints = CloudEndpoints.ForEnvironment(accountInfo.CloudEnvironment);

            Uri blobServiceUri = endpoints.GetStorageServiceUri(accountInfo.AccountName, AzureStorageService.Blob);
            Uri queueServiceUri = endpoints.GetStorageServiceUri(accountInfo.AccountName, AzureStorageService.Queue);
            Uri tableServiceUri = endpoints.GetStorageServiceUri(accountInfo.AccountName, AzureStorageService.Table);

            if (string.Equals(accountInfo.Credential, Credential.ManagedIdentity, StringComparison.OrdinalIgnoreCase))
            {
                TokenCredentialOptions options = new TokenCredentialOptions { AuthorityHost = endpoints.AuthorityHost };
                blobServiceClient = new BlobServiceClient(blobServiceUri, new ManagedIdentityCredential(accountInfo.ClientId, options));
                queueServiceClient = new QueueServiceClient(queueServiceUri, new ManagedIdentityCredential(accountInfo.ClientId, options));
                tableServiceClient = new TableServiceClient(tableServiceUri, new ManagedIdentityCredential(accountInfo.ClientId, options));
            }
            else
            {
                blobServiceClient = new BlobServiceClient(blobServiceUri);
                queueServiceClient = new QueueServiceClient(queueServiceUri);
                tableServiceClient = new TableServiceClient(tableServiceUri);
            }
        }
        else
        {
            blobServiceClient = new BlobServiceClient(accountInfo.ConnectionString);
            queueServiceClient = new QueueServiceClient(accountInfo.ConnectionString);
            tableServiceClient = new TableServiceClient(accountInfo.ConnectionString);
        }

        // Get task hub metadata
        await blobServiceClient.CreateBlobContainerAsync("", cancellationToken);
        var b = 
    }
}
