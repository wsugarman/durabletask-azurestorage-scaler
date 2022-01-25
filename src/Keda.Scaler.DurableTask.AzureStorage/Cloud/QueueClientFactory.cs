// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using EnsureThat;
using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.Cloud;

internal class QueueClientFactory
{
    private readonly QueueClientOptions _queueClientOptions;

    public QueueClientFactory(IOptionsSnapshot<QueueClientOptions> queueClientOptions)
    {
        _queueClientOptions = EnsureArg.IsNotNull(queueClientOptions?.Value, nameof(queueClientOptions));
    }
    public QueueServiceClient GetQueueServiceClient(string connectionString)
    {
        EnsureArg.IsNotEmptyOrWhiteSpace(connectionString, nameof(connectionString));
        return new QueueServiceClient(connectionString, _queueClientOptions);
    }

    public QueueServiceClient GetQueueServiceClient(string accountName, CloudEndpoints endpoints)
    {
        EnsureArg.IsNotEmptyOrWhiteSpace(accountName, nameof(accountName));
        EnsureArg.IsNotNull(endpoints, nameof(endpoints));

        // We assume there is only 1 managed identity present in the hosting environment
        TokenCredential credential = new ManagedIdentityCredential(options: new TokenCredentialOptions { AuthorityHost = endpoints.AuthorityHost });
        return new QueueServiceClient(CreateServiceEndpoint("https", accountName, "queue." + endpoints.StorageSuffix), credential, _queueClientOptions);
    }

    private static Uri CreateServiceEndpoint(string scheme, string accountName, string suffix)
        => new Uri($"{scheme}://{accountName}.{suffix}", UriKind.Absolute);
}
