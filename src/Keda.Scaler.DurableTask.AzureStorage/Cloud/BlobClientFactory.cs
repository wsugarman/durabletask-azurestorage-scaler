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

internal class BlobClientFactory
{
    private readonly BlobClientOptions _blobClientOptions;

    public BlobClientFactory(IOptionsSnapshot<BlobClientOptions> blobClientOptions)
    {
        _blobClientOptions = EnsureArg.IsNotNull(blobClientOptions?.Value, nameof(blobClientOptions));
    }

    public BlobServiceClient GetBlobServiceClient(string connectionString)
    {
        EnsureArg.IsNotEmptyOrWhiteSpace(connectionString, nameof(connectionString));
        return new BlobServiceClient(connectionString, _blobClientOptions);
    }

    public BlobServiceClient GetBlobServiceClient(string accountName, CloudEndpoints endpoints)
    {
        EnsureArg.IsNotEmptyOrWhiteSpace(accountName, nameof(accountName));
        EnsureArg.IsNotNull(endpoints, nameof(endpoints));

        // We assume there is only 1 managed identity present in the hosting environment
        TokenCredential credential = new ManagedIdentityCredential(options: new TokenCredentialOptions { AuthorityHost = endpoints.AuthorityHost });
        return new BlobServiceClient(CreateServiceEndpoint("https", accountName, "blob." + endpoints.StorageSuffix), credential, _blobClientOptions);
    }

    private static Uri CreateServiceEndpoint(string scheme, string accountName, string suffix)
        => new Uri($"{scheme}://{accountName}.{suffix}", UriKind.Absolute);
}
