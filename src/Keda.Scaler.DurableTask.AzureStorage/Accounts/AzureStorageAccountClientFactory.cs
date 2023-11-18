// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Keda.Scaler.DurableTask.AzureStorage.Extensions;

namespace Keda.Scaler.DurableTask.AzureStorage.Accounts;

internal abstract class AzureStorageAccountClientFactory<T> : IStorageAccountClientFactory<T>
{
    public T GetServiceClient(AzureStorageAccountInfo accountInfo)
    {
        ArgumentNullException.ThrowIfNull(accountInfo);

        if (string.IsNullOrWhiteSpace(accountInfo.ConnectionString))
        {
            if (string.IsNullOrWhiteSpace(accountInfo.AccountName))
                throw new ArgumentException(SR.Format(SR.MissingMemberFormat, nameof(accountInfo.AccountName)), nameof(accountInfo));

            if (accountInfo.Cloud is null)
                throw new ArgumentException(SR.Format(SR.MissingMemberFormat, nameof(accountInfo.Cloud)), nameof(accountInfo));

            AzureStorageService service = typeof(T) == typeof(BlobServiceClient) ? AzureStorageService.Blob : AzureStorageService.Queue;
            Uri serviceUri = accountInfo.Cloud.GetStorageServiceUri(accountInfo.AccountName, service);

            if (string.Equals(accountInfo.Credential, Credential.ManagedIdentity, StringComparison.OrdinalIgnoreCase))
            {
                ManagedIdentityCredential credential = new(
                    accountInfo.ClientId,
                    new TokenCredentialOptions { AuthorityHost = accountInfo.Cloud.AuthorityHost });

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
