// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Azure.Identity;
using Keda.Scaler.DurableTask.AzureStorage.Metadata;
using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.Clients;

internal sealed class ConfigureAzureStorageAccountOptions(IOptionsSnapshot<ScalerOptions> scalerOptions) : IConfigureOptions<AzureStorageAccountOptions>
{
    private readonly ScalerOptions _scalerOptions = scalerOptions?.Get(default) ?? throw new ArgumentNullException(nameof(scalerOptions));

    public void Configure(AzureStorageAccountOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (_scalerOptions.AccountName is null)
            ConfigureStringBasedConnection(options);
        else
            ConfigureUriBasedConnection(options);
    }

    private void ConfigureStringBasedConnection(AzureStorageAccountOptions options)
        => options.ConnectionString = _scalerOptions.Connection ?? Environment.GetEnvironmentVariable(_scalerOptions.ConnectionFromEnv ?? AzureStorageAccountOptions.DefaultConnectionEnvironmentVariable, EnvironmentVariableTarget.Process);

    private void ConfigureUriBasedConnection(AzureStorageAccountOptions options)
    {
        options.AccountName = _scalerOptions.AccountName;

        if (AzureCloudEndpoints.TryParseEnvironment(_scalerOptions.Cloud, out CloudEnvironment cloud))
        {
            AzureCloudEndpoints endpoints = cloud is CloudEnvironment.Private
                ? new AzureCloudEndpoints(_scalerOptions.EntraEndpoint!, _scalerOptions.EndpointSuffix!)
                : AzureCloudEndpoints.ForEnvironment(cloud);

            options.EndpointSuffix = endpoints.StorageSuffix;
            options.TokenCredential = CreateTokenCredential(endpoints.AuthorityHost);
        }
    }

    private WorkloadIdentityCredential? CreateTokenCredential(Uri authorityHost)
    {
        if (_scalerOptions.UseManagedIdentity)
        {
            WorkloadIdentityCredentialOptions options = new()
            {
                AuthorityHost = authorityHost,
            };

            if (!string.IsNullOrWhiteSpace(_scalerOptions.ClientId))
                options.ClientId = _scalerOptions.ClientId;

            return new WorkloadIdentityCredential(options);
        }

        return null;
    }
}
