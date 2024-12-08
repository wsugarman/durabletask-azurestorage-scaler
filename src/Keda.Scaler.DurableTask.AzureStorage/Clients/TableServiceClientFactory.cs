// Copyright © William Sugarman.
// Licensed under the MIT License.

using Azure.Core;
using Azure.Data.Tables;
using System;

namespace Keda.Scaler.DurableTask.AzureStorage.Clients;

internal sealed class TableServiceClientFactory : AzureStorageAccountClientFactory<TableServiceClient>
{
    protected override AzureStorageService Service => AzureStorageService.Table;

    protected override TableServiceClient CreateServiceClient(string connectionString)
        => new(connectionString);

    protected override TableServiceClient CreateServiceClient(Uri serviceUri, TokenCredential credential)
        => new(serviceUri, credential);
}
