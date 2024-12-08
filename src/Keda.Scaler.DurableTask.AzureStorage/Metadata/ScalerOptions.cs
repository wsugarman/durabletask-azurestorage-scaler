// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Keda.Scaler.DurableTask.AzureStorage.Metadata;

/// <summary>
/// Represents the metadata present in the KEDA ScaledObject resource that configures the
/// Durable Task Azure Storage external scaler.
/// </summary>
[DebuggerDisplay("{TaskHubName,nq}")]
public sealed class ScalerOptions
{
    /// <summary>
    /// Gets or sets the optional name of the Azure Storage account used by the Durable Task framework.
    /// </summary>
    /// <remarks>
    /// This value is only required if a connection string is not specified.
    /// </remarks>
    /// <value>The name of the Azure Storage account if specified; otherwise, <see langword="null"/>.</value>
    public string? AccountName { get; set; }

    /// <summary>
    /// Gets or sets the optional client id to be used when authenticating with managed identity.
    /// </summary>
    /// <remarks>
    /// This value is only required if <see cref="UseManagedIdentity"/> is <see langword="true"/>.
    /// If managed identity is specified, but the <see cref="ClientId"/> is left unspecified, then the default
    /// identity is chosen. Be sure to specify a client id if there are multiple identities.
    /// </remarks>
    /// <value>The client id to be used with managed identity.</value>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the name of the cloud environment that contains the Azure Storage account used by the Durable Task framework.
    /// </summary>
    /// <value>The name of the Azure cloud environment containing the storage account.</value>
    public string? Cloud { get; set; }

    /// <summary>
    /// Gets or sets the optional connection string for the Azure Storage account used by the Durable Task framework.
    /// </summary>
    /// <remarks>
    /// This value is only required if <see cref="AccountName"/> and <see cref="ConnectionFromEnv"/>
    /// are not specified.
    /// </remarks>
    /// <value>
    /// A collection of semicolon-delimited properties used to connect to Azure Storage if specified;
    /// otherwise, <see langword="null"/>.
    /// </value>
    public string? Connection { get; set; }

    /// <summary>
    /// Gets or sets the optional name of the environment variable whose value is the connection string
    /// for the Azure Storage account used by the Durable Task framework.
    /// </summary>
    /// <remarks>
    /// This value is only required if <see cref="AccountName"/> and <see cref="Connection"/>
    /// are not specified.
    /// </remarks>
    /// <value>The name of an environment variable in the deployment if specified; otherwise, <see langword="null"/>.</value>
    public string? ConnectionFromEnv { get; set; }

    /// <summary>
    /// Gets or sets the optional Azure Storage host suffix for the Azure cloud.
    /// </summary>
    /// <remarks>
    /// This value may only be specified when <see cref="AccountName"/> is specified.
    /// </remarks>
    /// <value>The cloud's optional suffix for Azure Storage endpoints.</value>
    public string? EndpointSuffix { get; set; }

    /// <summary>
    /// Gets or sets the optional URL from which tokens may be requested for private clouds.
    /// </summary>
    /// <remarks>
    /// This value may only be specified when the value of <see cref="Cloud"/> is <c>"private"</c>.
    /// </remarks>
    /// <value>The private cloud's host authority for Microsoft Entra ID (formally Azure Active Directory).</value>
    public Uri? EntraEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of activity work items that a single worker may process at any time.
    /// </summary>
    /// <value>The positive number of work items.</value>
    [Range(1, int.MaxValue)]
    public int MaxActivitiesPerWorker { get; set; } = 10;

    /// <summary>
    /// Gets or sets the maximum number of orchestration work items that a single worker may process at any time.
    /// </summary>
    /// <value>The positive number of work items.</value>
    [Range(1, int.MaxValue)]
    public int MaxOrchestrationsPerWorker { get; set; } = 5;

    /// <summary>
    /// Gets or sets the name of the configured task hub present in Azure Storage.
    /// </summary>
    /// <remarks>If unspecified, the default name is <c>"TestHubName"</c>.</remarks>
    /// <value>The name of the task hub.</value>
    [Required]
    public string TaskHubName { get; set; } = "TestHubName";

    /// <summary>
    /// Gets or sets a value indicating whether the newer and more reliable partition manager, that relies upon the Azure Table Service,
    /// should be used instead of the older legacy partition manager, that relies upon the Azure Blob Service.
    /// </summary>
    /// <remarks>
    /// This feature is only available in v2.10.0 and beyond of the Durable Functions extension.
    /// </remarks>
    /// <value>
    /// <see langword="true"/> if the newer table-based partition manager should be used;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool UseTablePartitionManagement { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether a federated identity should be used to authenticate the connection to Azure Storage.
    /// </summary>
    /// <remarks>
    /// Workload identity cannot be used with connection strings. IMDS is not supported.
    /// </remarks>
    /// <value>
    /// <see langword="true"/> if a federated identity is available in the service pod and should be used to authenticate;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool UseManagedIdentity { get; set; }
}
