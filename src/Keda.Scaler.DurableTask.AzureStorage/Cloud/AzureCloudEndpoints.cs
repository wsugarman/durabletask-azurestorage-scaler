// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Azure.Identity;

namespace Keda.Scaler.DurableTask.AzureStorage.Cloud;

/// <summary>
/// Represents the various Azure service endpoints for a given environment.
/// </summary>
public sealed class AzureCloudEndpoints
{
    /// <summary>
    /// Gets the Azure service endpoints for the public Azure cloud.
    /// </summary>
    /// <value>An instance of <see cref="AzureCloudEndpoints"/> for the public Azure cloud.</value>
    public static AzureCloudEndpoints Public { get; } = new AzureCloudEndpoints(AzureAuthorityHosts.AzurePublicCloud, "core.windows.net");

    /// <summary>
    /// Gets the Azure service endpoints for the US government Azure cloud.
    /// </summary>
    /// <value>An instance of <see cref="AzureCloudEndpoints"/> for the US government Azure cloud.</value>
    public static AzureCloudEndpoints USGovernment { get; } = new AzureCloudEndpoints(AzureAuthorityHosts.AzureGovernment, "core.usgovcloudapi.net");

    /// <summary>
    /// Gets the Azure service endpoints for the Chinese Azure cloud.
    /// </summary>
    /// <value>An instance of <see cref="AzureCloudEndpoints"/> for the Chinese Azure cloud.</value>
    public static AzureCloudEndpoints China { get; } = new AzureCloudEndpoints(AzureAuthorityHosts.AzureChina, "core.chinacloudapi.cn");

    /// <summary>
    /// Gets the Azure service endpoints for the German Azure cloud.
    /// </summary>
    /// <value>An instance of <see cref="AzureCloudEndpoints"/> for the German Azure cloud.</value>
    public static AzureCloudEndpoints Germany { get; } = new AzureCloudEndpoints(AzureAuthorityHosts.AzureGermany, "core.cloudapi.de");

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
    /// Initializes a new instance of the <see cref="AzureCloudEndpoints"/> class with the specified metadata.
    /// </summary>
    /// <param name="authorityHost">The Azure Active Directory (AAD) authority URL.</param>
    /// <param name="storageSuffix">The Azure Storage service endpoint suffix.</param>
    /// <exception cref="ArgumentNullException"><paramref name="storageSuffix"/> is <see langword="null"/>.</exception>
    public AzureCloudEndpoints(Uri authorityHost, string storageSuffix)
    {
        ArgumentNullException.ThrowIfNull(authorityHost);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageSuffix);

        AuthorityHost = authorityHost;
        StorageSuffix = storageSuffix;
    }

    /// <summary>
    /// Retrieves the endpoints for the given <paramref name="cloud"/>.
    /// </summary>
    /// <param name="cloud">An Azure cloud environment.</param>
    /// <returns>
    /// An instance of <see cref="AzureCloudEndpoints"/> whose values correspond to the given <paramref name="cloud"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static AzureCloudEndpoints ForEnvironment(CloudEnvironment cloud)
    {
        return cloud switch
        {
            CloudEnvironment.AzurePublicCloud => Public,
            CloudEnvironment.AzureUSGovernmentCloud => USGovernment,
            CloudEnvironment.AzureChinaCloud => China,
            CloudEnvironment.AzureGermanCloud => Germany,
            _ => throw new ArgumentOutOfRangeException(nameof(cloud)),
        };
    }
}
