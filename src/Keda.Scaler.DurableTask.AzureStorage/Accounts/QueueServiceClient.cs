// Copyright © William Sugarman.
// Licensed under the MIT License.

using Azure.Core;
using Azure.Storage.Queues;
using System;

namespace Keda.Scaler.DurableTask.AzureStorage.Accounts;

internal sealed class QueueServiceClientFactory : AzureStorageAccountClientFactory<QueueServiceClient>
{
    protected override QueueServiceClient CreateServiceClient(string connectionString)
        => new QueueServiceClient(connectionString);

    protected override QueueServiceClient CreateServiceClient(Uri serviceUri)
        => new QueueServiceClient(serviceUri);

    protected override QueueServiceClient CreateServiceClient(Uri serviceUri, TokenCredential credential)
        => new QueueServiceClient(serviceUri, credential);
}
