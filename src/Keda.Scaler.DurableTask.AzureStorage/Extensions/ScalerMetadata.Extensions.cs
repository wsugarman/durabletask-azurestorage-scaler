// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.Accounts;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Keda.Scaler.DurableTask.AzureStorage.Common;

namespace Keda.Scaler.DurableTask.AzureStorage.Extensions;

internal static class ScalerMetadataExtensions
{
    public static AzureStorageAccountInfo GetAccountInfo(this ScalerMetadata scalerMetadata, IProcessEnvironment environment)
    {
        if (scalerMetadata is null)
            throw new ArgumentNullException(nameof(scalerMetadata));

        if (environment is null)
            throw new ArgumentNullException(nameof(environment));

        return new AzureStorageAccountInfo
        {
            AccountName = scalerMetadata.AccountName,
            ClientId = scalerMetadata.ClientId,
            Cloud = scalerMetadata.CloudEnvironment switch
            {
                CloudEnvironment.Unknown => null,
                CloudEnvironment.Private => new CloudEndpoints(scalerMetadata.ActiveDirectoryEndpoint!, scalerMetadata.EndpointSuffix!),
                _ => CloudEndpoints.ForEnvironment(scalerMetadata.CloudEnvironment),
            },
            ConnectionString = scalerMetadata.ResolveConnectionString(environment),
            Credential = scalerMetadata.UseManagedIdentity ? Credential.ManagedIdentity : null,
        };
    }
}
