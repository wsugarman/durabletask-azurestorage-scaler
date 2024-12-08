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
        options.EndpointSuffix = GetEndpointSuffix();
        options.TokenCredential = CreateTokenCredential();
    }

    private string? GetEndpointSuffix()
    {
        if (_scalerOptions.Cloud is null || _scalerOptions.Cloud.Equals(CloudEnvironment.AzurePublicCloud, StringComparison.OrdinalIgnoreCase))
            return AzureStorageServiceUri.PublicSuffix;
        else if (_scalerOptions.Cloud.Equals(CloudEnvironment.AzureUSGovernmentCloud, StringComparison.OrdinalIgnoreCase))
            return AzureStorageServiceUri.USGovernmentSuffix;
        else if (_scalerOptions.Cloud.Equals(CloudEnvironment.AzureChinaCloud, StringComparison.OrdinalIgnoreCase))
            return AzureStorageServiceUri.ChinaSuffix;
        else if (_scalerOptions.Cloud.Equals(CloudEnvironment.Private, StringComparison.OrdinalIgnoreCase))
            return _scalerOptions.EndpointSuffix;
        else
            return null;
    }

    private Uri? GetAuthorityHost()
    {
        if (_scalerOptions.Cloud is null || _scalerOptions.Cloud.Equals(CloudEnvironment.AzurePublicCloud, StringComparison.OrdinalIgnoreCase))
            return AzureAuthorityHosts.AzurePublicCloud;
        else if (_scalerOptions.Cloud.Equals(CloudEnvironment.AzureUSGovernmentCloud, StringComparison.OrdinalIgnoreCase))
            return AzureAuthorityHosts.AzureGovernment;
        else if (_scalerOptions.Cloud.Equals(CloudEnvironment.AzureChinaCloud, StringComparison.OrdinalIgnoreCase))
            return AzureAuthorityHosts.AzureChina;
        else if (_scalerOptions.Cloud.Equals(CloudEnvironment.Private, StringComparison.OrdinalIgnoreCase))
            return _scalerOptions.EntraEndpoint;
        else
            return null;
    }

    private WorkloadIdentityCredential? CreateTokenCredential()
    {
        if (_scalerOptions.UseManagedIdentity)
        {
            WorkloadIdentityCredentialOptions options = new()
            {
                AuthorityHost = GetAuthorityHost(),
            };

            if (!string.IsNullOrWhiteSpace(_scalerOptions.ClientId))
                options.ClientId = _scalerOptions.ClientId;

            return new WorkloadIdentityCredential(options);
        }

        return null;
    }
}
