// Copyright © William Sugarman.
// Licensed under the MIT License.

using Azure.Identity;
using System;
using Azure.Storage.Blobs;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Keda.Scaler.DurableTask.AzureStorage.Extensions;
using Azure.Core;

namespace Keda.Scaler.DurableTask.AzureStorage.Accounts;

internal abstract class AzureStorageAccountClientFactory<T> : IStorageAccountClientFactory<T>
{
    public T GetServiceClient(AzureStorageAccountInfo accountInfo)
    {
        if (accountInfo is null)
            throw new ArgumentNullException(nameof(accountInfo));

        if (string.IsNullOrWhiteSpace(accountInfo.ConnectionString))
        {
            if (string.IsNullOrWhiteSpace(accountInfo.AccountName))
                throw new ArgumentException(SR.Format(SR.MissingMemberFormat, nameof(accountInfo.AccountName)), nameof(accountInfo));

            if (accountInfo.CloudEnvironment == CloudEnvironment.Unknown)
                throw new ArgumentException(SR.Format(SR.MissingMemberFormat, nameof(accountInfo.CloudEnvironment)), nameof(accountInfo));

            AzureStorageService service = typeof(T) == typeof(BlobServiceClient) ? AzureStorageService.Blob : AzureStorageService.Queue;
            CloudEndpoints endpoints = CloudEndpoints.ForEnvironment(accountInfo.CloudEnvironment);
            Uri serviceUri = endpoints.GetStorageServiceUri(accountInfo.AccountName, service);

            if (string.Equals(accountInfo.Credential, Credential.ManagedIdentity, StringComparison.OrdinalIgnoreCase))
            {
                ManagedIdentityCredential credential = new ManagedIdentityCredential(
                    accountInfo.ClientId,
                    new TokenCredentialOptions { AuthorityHost = endpoints.AuthorityHost });

                return CreateServiceClient(serviceUri, credential);
            }
            else
            {
                return CreateServiceClient(serviceUri);
            }
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
