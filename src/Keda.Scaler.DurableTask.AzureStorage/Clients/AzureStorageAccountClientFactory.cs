// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Azure.Core;
using Keda.Scaler.DurableTask.AzureStorage.Accounts;
using Keda.Scaler.DurableTask.AzureStorage.Clouds;

namespace Keda.Scaler.DurableTask.AzureStorage.Clients;

internal abstract class AzureStorageAccountClientFactory<T> : IStorageAccountClientFactory<T>
{
    protected abstract AzureStorageService Service { get; }

    public T GetServiceClient(AzureStorageAccountOptions accountInfo)
    {
        ArgumentNullException.ThrowIfNull(accountInfo);

        if (string.IsNullOrWhiteSpace(accountInfo.ConnectionString))
        {
            Uri serviceUri = AzureStorageEndpoint.GetStorageServiceUri(accountInfo.AccountName!, Service, accountInfo.EndpointSuffix!);
            return accountInfo.TokenCredential is null ? CreateServiceClient(serviceUri) : CreateServiceClient(serviceUri, accountInfo.TokenCredential);
        }
        else
        {
            return CreateServiceClient(accountInfo.ConnectionString);
        }
    }

    protected abstract T CreateServiceClient(string connectionString);

    protected abstract T CreateServiceClient(Uri serviceUri);

    protected abstract T CreateServiceClient(Uri serviceUri, TokenCredential credential);
}
