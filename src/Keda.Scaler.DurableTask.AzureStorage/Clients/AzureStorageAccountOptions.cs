// Copyright © William Sugarman.
// Licensed under the MIT License.

using Azure.Identity;
using Keda.Scaler.DurableTask.AzureStorage.Metadata;

namespace Keda.Scaler.DurableTask.AzureStorage.Clients;

/// <summary>
/// Represents a collection of metadata that specifies the connection for a particular Azure Storage account.
/// </summary>
public sealed class AzureStorageAccountOptions
{
    internal const string DefaultConnectionEnvironmentVariable = "AzureWebJobsStorage";

    /// <inheritdoc cref="ScalerOptions.AccountName"/>
    public string? AccountName { get; set; }

    /// <summary>
    /// Gets the optional connection string for Azure Storage.
    /// </summary>
    /// <value>The Azure Storage connection string if specified; otherwise <see langword="null"/>.</value>
    public string? ConnectionString { get; set; }

    /// <inheritdoc cref="ScalerOptions.EndpointSuffix"/>
    public string? EndpointSuffix { get; set; }

    /// <summary>
    /// Gets or sets the optional token credential used for managed identity within a Kubernetes cluster.
    /// </summary>
    /// <value>An optional <see cref="WorkloadIdentityCredential"/> if specified; otherwise, <see langword="null"/>.</value>
    public WorkloadIdentityCredential? TokenCredential { get; set; }
}
