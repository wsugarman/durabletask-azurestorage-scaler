// Copyright © William Sugarman.
// Licensed under the MIT License.

using Azure.Core;
using Azure.Storage.Blobs;
using System;

namespace Keda.Scaler.DurableTask.AzureStorage.Clients;

internal sealed class BlobServiceClientFactory : AzureStorageAccountClientFactory<BlobServiceClient>
{
    protected override AzureStorageService Service => AzureStorageService.Blob;

    protected override BlobServiceClient CreateServiceClient(string connectionString)
        => new(connectionString);

    protected override BlobServiceClient CreateServiceClient(Uri serviceUri)
        => new(serviceUri);

    protected override BlobServiceClient CreateServiceClient(Uri serviceUri, TokenCredential credential)
        => new(serviceUri, credential);
}
