// Copyright © William Sugarman.
// Licensed under the MIT License.

using Azure.Core;
using Azure.Storage.Queues;
using System;

namespace Keda.Scaler.DurableTask.AzureStorage.Clients;

internal sealed class QueueServiceClientFactory : AzureStorageAccountClientFactory<QueueServiceClient>
{
    protected override AzureStorageService Service => AzureStorageService.Queue;

    protected override QueueServiceClient CreateServiceClient(string connectionString)
        => new(connectionString);

    protected override QueueServiceClient CreateServiceClient(Uri serviceUri, TokenCredential credential)
        => new(serviceUri, credential);
}
