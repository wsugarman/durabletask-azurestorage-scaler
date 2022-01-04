// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Azure.Identity;

namespace Keda.Scaler.DurableTask.AzureStorage.Cloud;

/// <summary>
/// Represents the various Azure service endpoints for a given environment.
/// </summary>
public sealed class CloudEndpoints
{
    /// <summary>
    /// Gets the Azure service endpoints for the public Azure cloud.
    /// </summary>
    /// <value>An instance of <see cref="CloudEndpoints"/> for the public Azure cloud.</value>
    public static CloudEndpoints Public { get; } = new CloudEndpoints();

    /// <summary>
    /// Gets the Azure service endpoints for the US government Azure cloud.
    /// </summary>
    /// <value>An instance of <see cref="CloudEndpoints"/> for the US government Azure cloud.</value>
    public static CloudEndpoints USGovernment { get; } = new CloudEndpoints
    {
        AuthorityHost = AzureAuthorityHosts.AzureGovernment,
        StorageSuffix = "core.usgovcloudapi.net",
    };

    /// <summary>
    /// Gets the Azure service endpoints for the Chinese Azure cloud.
    /// </summary>
    /// <value>An instance of <see cref="CloudEndpoints"/> for the Chinese Azure cloud.</value>
    public static CloudEndpoints China { get; } = new CloudEndpoints
    {
        AuthorityHost = AzureAuthorityHosts.AzureChina,
        StorageSuffix = "core.chinacloudapi.cn",
    };

    /// <summary>
    /// Gets the Azure service endpoints for the German Azure cloud.
    /// </summary>
    /// <value>An instance of <see cref="CloudEndpoints"/> for the German Azure cloud.</value>
    public static CloudEndpoints Germany { get; } = new CloudEndpoints
    {
        AuthorityHost = AzureAuthorityHosts.AzureGermany,
        StorageSuffix = "core.cloudapi.de",
    };

    // TODO: Add private cloud suffix(es)

    /// <summary>
    /// Gets the Azure Active Directory (AAD) authority URL.
    /// </summary>
    /// <value>The AAD authority.</value>
    public Uri AuthorityHost { get; private init; } = AzureAuthorityHosts.AzurePublicCloud;

    /// <summary>
    /// Gets the Azure Storage service endpoint suffix.
    /// </summary>
    /// <value>The suffix for all Azure Storage service endpoints.</value>
    public string StorageSuffix { get; private init; } = "core.windows.net";

    /// <summary>
    /// Retrieves the endpoints for the given <paramref name="cloud"/>.
    /// </summary>
    /// <param name="cloud">An Azure cloud environment.</param>
    /// <returns>
    /// An instance of <see cref="CloudEndpoints"/> whose values correspond to the given <paramref name="cloud"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static CloudEndpoints ForEnvironment(CloudEnvironment cloud)
        => cloud switch
        {
            CloudEnvironment.AzurePublicCloud => Public,
            CloudEnvironment.AzureUSGovernmentCloud => USGovernment,
            CloudEnvironment.AzureChinaCloud => China,
            CloudEnvironment.AzureGermanCloud => Germany,
            _ => throw new ArgumentOutOfRangeException(nameof(cloud)),
        };
}
