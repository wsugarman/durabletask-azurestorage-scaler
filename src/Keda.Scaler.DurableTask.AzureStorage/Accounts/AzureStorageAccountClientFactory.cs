// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;

namespace Keda.Scaler.DurableTask.AzureStorage.Accounts;

internal abstract class AzureStorageAccountClientFactory<T> : IStorageAccountClientFactory<T>
{
    public T GetServiceClient(AzureStorageAccountInfo accountInfo)
    {
        ArgumentNullException.ThrowIfNull(accountInfo);

        if (string.IsNullOrWhiteSpace(accountInfo.ConnectionString))
        {
            if (string.IsNullOrWhiteSpace(accountInfo.AccountName))
                throw new ArgumentException(Resource.Format(SRF.MissingMemberFormat, nameof(AzureStorageAccountInfo.AccountName)), nameof(accountInfo));

            if (accountInfo.Cloud is null)
                throw new ArgumentException(Resource.Format(SRF.MissingMemberFormat, nameof(AzureStorageAccountInfo.Cloud)), nameof(accountInfo));

            AzureStorageService service = typeof(T) == typeof(BlobServiceClient) ? AzureStorageService.Blob : AzureStorageService.Queue;
            Uri serviceUri = accountInfo.Cloud.GetStorageServiceUri(accountInfo.AccountName, service);

#pragma warning disable CS0618 // Type or member is obsolete
            if (string.Equals(accountInfo.Credential, Credentials.WorkloadIdentity, StringComparison.OrdinalIgnoreCase))
            {
                WorkloadIdentityCredentialOptions options = new() { AuthorityHost = accountInfo.Cloud.AuthorityHost };

                // Optionally override the service account annotation 'azure.workload.identity/client-id'
                if (accountInfo.ClientId is not null)
                    options.ClientId = accountInfo.ClientId;

                return CreateServiceClient(serviceUri, new WorkloadIdentityCredential(options));
            }
            else if (string.Equals(accountInfo.Credential, Credentials.ManagedIdentity, StringComparison.OrdinalIgnoreCase))
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
#pragma warning restore CS0618 // Type or member is obsolete
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
