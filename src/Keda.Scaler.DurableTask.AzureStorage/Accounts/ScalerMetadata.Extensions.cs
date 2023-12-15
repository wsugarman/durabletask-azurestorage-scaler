// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;

namespace Keda.Scaler.DurableTask.AzureStorage.Accounts;

internal static class ScalerMetadataExtensions
{
    public static AzureStorageAccountInfo GetAccountInfo(this ScalerMetadata scalerMetadata)
    {
        ArgumentNullException.ThrowIfNull(scalerMetadata);

        return new AzureStorageAccountInfo
        {
            AccountName = scalerMetadata.AccountName,
            ClientId = scalerMetadata.ClientId,
            Cloud = scalerMetadata.CloudEnvironment switch
            {
                CloudEnvironment.Unknown => null,
                CloudEnvironment.Private => new AzureCloudEndpoints(scalerMetadata.ActiveDirectoryEndpoint!, scalerMetadata.EndpointSuffix!),
                _ => AzureCloudEndpoints.ForEnvironment(scalerMetadata.CloudEnvironment),
            },
            ConnectionString = scalerMetadata.ConnectionString,
            Credential = GetCredential(scalerMetadata),
        };
    }

    private static string? GetCredential(this ScalerMetadata scalerMetadata)
    {
        if (scalerMetadata.UseWorkloadIdentity)
            return Credentials.WorkloadIdentity;

#pragma warning disable CS0618 // Type or member is obsolete
        if (scalerMetadata.UseManagedIdentity)
            return Credentials.ManagedIdentity;
#pragma warning restore CS0618 // Type or member is obsolete

        return null;
    }
}
