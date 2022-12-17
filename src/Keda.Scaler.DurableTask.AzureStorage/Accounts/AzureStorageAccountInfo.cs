// Copyright © William Sugarman.
// Licensed under the MIT License.

using Keda.Scaler.DurableTask.AzureStorage.Cloud;

namespace Keda.Scaler.DurableTask.AzureStorage.Accounts;

/// <summary>
/// Represents a collection of metadata that specifies the connection for a particular Azure Storage account.
/// </summary>
public sealed class AzureStorageAccountInfo
{
    /// <summary>
    /// Gets the optional name of the Azure Storage account.
    /// </summary>
    /// <remarks>
    /// This value is only required if <see cref="ConnectionString"/> is not specified.
    /// </remarks>
    /// <value>The name of the Azure Storage account if specified; otherwise, <see langword="null"/>.</value>
    public string? AccountName { get; init; }

    /// <summary>
    /// Gets the optional client id to be used when authenticating with managed identity.
    /// </summary>
    /// <remarks>
    /// This value is only required if <see cref="Credential"/> is <c>"ManagedIdentity"</c>.
    /// If managed identity is specified, but the <see cref="ClientId"/> is left unspecified, then a default
    /// identity is chosen. Be sure to specify a client id if there are multiple identities.
    /// </remarks>
    /// <value>The client id to be used with managed identity.</value>
    public string? ClientId { get; init; }

    /// <summary>
    /// Gets the cloud environment that contains the Azure Storage account.
    /// </summary>
    /// <value>
    /// The Azure cloud environment containing the storage account, if a known value;
    /// otherwise, <see cref="CloudEnvironment.Unknown"/>.
    /// </value>
    public CloudEnvironment CloudEnvironment { get; init; }

    /// <summary>
    /// Gets the optional connection string for the Azure Storage account.
    /// </summary>
    /// <remarks>
    /// This value is only required if <see cref="AccountName"/> is not specified.
    /// </remarks>
    /// <value>
    /// A collection of semicolon-delimited properties used to connect to the Azure Storage service if specified;
    /// otherwise, <see langword="null"/>.
    /// </value>
    public string? ConnectionString { get; init; }

    /// <summary>
    /// Gets the optional moniker for the credential used to connect to the Azure Storage account.
    /// </summary>
    /// <remarks>
    /// This value may only be specified if <see cref="AccountName"/> is specified. If specified,
    /// <c>"ManagedIdentity"</c> if the only recognized value.
    /// </remarks>
    /// <value>The name of the credential to use if specified; otherwise, <see langword="null"/>.</value>
    public string? Credential { get; init; }
}
