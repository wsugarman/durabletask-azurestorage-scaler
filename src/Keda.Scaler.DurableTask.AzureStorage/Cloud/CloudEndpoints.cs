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
    public static CloudEndpoints Public { get; } = new CloudEndpoints(AzureAuthorityHosts.AzurePublicCloud, "core.windows.net");

    /// <summary>
    /// Gets the Azure service endpoints for the US government Azure cloud.
    /// </summary>
    /// <value>An instance of <see cref="CloudEndpoints"/> for the US government Azure cloud.</value>
    public static CloudEndpoints USGovernment { get; } = new CloudEndpoints(AzureAuthorityHosts.AzureGovernment, "core.usgovcloudapi.net");

    /// <summary>
    /// Gets the Azure service endpoints for the Chinese Azure cloud.
    /// </summary>
    /// <value>An instance of <see cref="CloudEndpoints"/> for the Chinese Azure cloud.</value>
    public static CloudEndpoints China { get; } = new CloudEndpoints(AzureAuthorityHosts.AzureChina, "core.chinacloudapi.cn");

    /// <summary>
    /// Gets the Azure service endpoints for the German Azure cloud.
    /// </summary>
    /// <value>An instance of <see cref="CloudEndpoints"/> for the German Azure cloud.</value>
    public static CloudEndpoints Germany { get; } = new CloudEndpoints(AzureAuthorityHosts.AzureGermany, "core.cloudapi.de");

    /// <summary>
    /// Gets the Azure Active Directory (AAD) authority URL.
    /// </summary>
    /// <value>The AAD authority.</value>
    public Uri AuthorityHost { get; }

    /// <summary>
    /// Gets the Azure Storage service endpoint suffix.
    /// </summary>
    /// <value>The suffix for all Azure Storage service endpoints.</value>
    public string StorageSuffix { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CloudEndpoints"/> class with the specified metadata.
    /// </summary>
    /// <param name="authorityHost">The Azure Active Directory (AAD) authority URL.</param>
    /// <param name="storageSuffix">The Azure Storage service endpoint suffix.</param>
    /// <exception cref="ArgumentNullException"><paramref name="storageSuffix"/> is <see langword="null"/>.</exception>
    public CloudEndpoints(Uri authorityHost, string storageSuffix)
    {
        AuthorityHost = authorityHost ?? throw new ArgumentNullException(nameof(authorityHost));
        StorageSuffix = storageSuffix ?? throw new ArgumentNullException(nameof(storageSuffix));
    }

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
