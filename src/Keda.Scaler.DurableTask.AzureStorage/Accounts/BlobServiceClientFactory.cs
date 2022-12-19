// Copyright © William Sugarman.
// Licensed under the MIT License.

using Azure.Core;
using Azure.Storage.Blobs;
using System;

namespace Keda.Scaler.DurableTask.AzureStorage.Accounts;

internal sealed class BlobServiceClientFactory : AzureStorageAccountClientFactory<BlobServiceClient>
{
    protected override BlobServiceClient CreateServiceClient(string connectionString)
        => new BlobServiceClient(connectionString);

    protected override BlobServiceClient CreateServiceClient(Uri serviceUri)
        => new BlobServiceClient(serviceUri);

    protected override BlobServiceClient CreateServiceClient(Uri serviceUri, TokenCredential credential)
        => new BlobServiceClient(serviceUri, credential);
}
