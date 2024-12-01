// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Azure.Identity;
using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.Clients;

internal sealed class ConfigureAzureStorageAccountOptions(IScalerMetadataAccessor accessor) : IConfigureOptions<AzureStorageAccountOptions>
{
    private readonly IScalerMetadataAccessor _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));

    public void Configure(AzureStorageAccountOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ScalerMetadata metadata = _accessor.ScalerMetadata ?? throw new InvalidOperationException(SR.ScalerMetadataNotFound);

        if (metadata.AccountName is null)
            ConfigureStringBasedConnection(metadata, options);
        else
            ConfigureUriBasedConnection(metadata, options);
    }

    private static void ConfigureStringBasedConnection(ScalerMetadata src, AzureStorageAccountOptions dst)
        => dst.ConnectionString = src.Connection ?? Environment.GetEnvironmentVariable(src.ConnectionFromEnv ?? AzureStorageAccountOptions.DefaultConnectionEnvironmentVariable, EnvironmentVariableTarget.Process);

    private static void ConfigureUriBasedConnection(ScalerMetadata src, AzureStorageAccountOptions dst)
    {
        dst.AccountName = src.AccountName;
        dst.EndpointSuffix = GetEndpointSuffix(src);
        dst.TokenCredential = CreateTokenCredential(src);
    }

    private static string? GetEndpointSuffix(ScalerMetadata metadata)
    {
        if (metadata.Cloud is null || metadata.Cloud.Equals(CloudEnvironment.AzurePublicCloud, StringComparison.OrdinalIgnoreCase))
            return AzureStorageServiceUri.PublicSuffix;
        else if (metadata.Cloud.Equals(CloudEnvironment.AzureUSGovernmentCloud, StringComparison.OrdinalIgnoreCase))
            return AzureStorageServiceUri.USGovernmentSuffix;
        else if (metadata.Cloud.Equals(CloudEnvironment.AzureChinaCloud, StringComparison.OrdinalIgnoreCase))
            return AzureStorageServiceUri.ChinaSuffix;
        else if (metadata.Cloud.Equals(CloudEnvironment.Private, StringComparison.OrdinalIgnoreCase))
            return metadata.EndpointSuffix;
        else
            return null;
    }

    private static WorkloadIdentityCredential? CreateTokenCredential(ScalerMetadata metadata)
    {
        if (metadata.UseManagedIdentity)
        {
            WorkloadIdentityCredentialOptions options = new()
            {
                AuthorityHost = metadata.EntraEndpoint,
                ClientId = metadata.ClientId,
            };

            return new WorkloadIdentityCredential(options);
        }

        return null;
    }
}
