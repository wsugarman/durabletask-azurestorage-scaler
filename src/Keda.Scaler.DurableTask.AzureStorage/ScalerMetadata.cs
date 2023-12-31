// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;

namespace Keda.Scaler.DurableTask.AzureStorage;

// TODO: Move back to init-properties once source generators support the 'init' keyword

/// <summary>
/// Represents the metadata present in the KEDA ScaledObject resource that configures the
/// Durable Task Azure Storage external scaler.
/// </summary>
public sealed class ScalerMetadata : IValidatableObject
{
    internal const string DefaultConnectionEnvironmentVariable = "AzureWebJobsStorage";

    /// <summary>
    /// Gets the optional name of the Azure Storage account used by the Durable Task framework.
    /// </summary>
    /// <remarks>
    /// This value is only required if <see cref="Connection"/> and <see cref="ConnectionFromEnv"/>
    /// are not specified.
    /// </remarks>
    /// <value>The name of the Azure Storage account if specified; otherwise, <see langword="null"/>.</value>
    public string? AccountName { get; set; }

    /// <summary>
    /// Gets the optional URL from which tokens may be requested for private clouds.
    /// </summary>
    /// <remarks>
    /// This value may only be specified when the value of <see cref="Cloud"/> is <c>"private"</c>.
    /// </remarks>
    /// <value>The private cloud's host authority for Azure Active Directory (AAD).</value>
    public Uri? ActiveDirectoryEndpoint { get; set; }

    /// <summary>
    /// Gets the optional client id to be used when authenticating with managed identity.
    /// </summary>
    /// <remarks>
    /// This value is only required if <see cref="UseManagedIdentity"/> is <see langword="true"/>.
    /// If managed identity is specified, but the <see cref="ClientId"/> is left unspecified, then a default
    /// identity is chosen. Be sure to specify a client id if there are multiple identities.
    /// </remarks>
    /// <value>The client id to be used with managed identity.</value>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets the cloud environment that contains the Azure Storage account used by the Durable Task framework.
    /// </summary>
    /// <value>
    /// The Azure cloud environment containing the storage account, if a known value;
    /// otherwise, <see cref="CloudEnvironment.Unknown"/>.
    /// </value>
    public CloudEnvironment CloudEnvironment
    {
        get
        {
            // Note: Do not use Enum.TryParse as it will accept numeric values
            if (Cloud is null || Cloud.Equals(nameof(CloudEnvironment.AzurePublicCloud), StringComparison.OrdinalIgnoreCase))
                return CloudEnvironment.AzurePublicCloud;
            else if (Cloud.Equals(nameof(CloudEnvironment.Private), StringComparison.OrdinalIgnoreCase))
                return CloudEnvironment.Private;
            else if (Cloud.Equals(nameof(CloudEnvironment.AzureUSGovernmentCloud), StringComparison.OrdinalIgnoreCase))
                return CloudEnvironment.AzureUSGovernmentCloud;
            else if (Cloud.Equals(nameof(CloudEnvironment.AzureChinaCloud), StringComparison.OrdinalIgnoreCase))
                return CloudEnvironment.AzureChinaCloud;
            else if (Cloud.Equals(nameof(CloudEnvironment.AzureGermanCloud), StringComparison.OrdinalIgnoreCase))
                return CloudEnvironment.AzureGermanCloud;
            else
                return CloudEnvironment.Unknown;
        }
    }

    /// <summary>
    /// Gets the name of the cloud environment that contains the Azure Storage account used by the Durable Task framework.
    /// </summary>
    /// <value>The name of the Azure cloud environment containing the storage account.</value>
    public string? Cloud { get; set; }

    /// <summary>
    /// Gets the optional connection string for the Azure Storage account used by the Durable Task framework.
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
    /// Gets the optional name of the environment variable whose value is the connection string
    /// for the Azure Storage account used by the Durable Task framework.
    /// </summary>
    /// <remarks>
    /// This value is only required if <see cref="AccountName"/> and <see cref="Connection"/>
    /// are not specified.
    /// </remarks>
    /// <value>The name of an environment variable in the deployment if specified; otherwise, <see langword="null"/>.</value>
    public string? ConnectionFromEnv { get; set; }

    /// <summary>
    /// Gets the optional connection string from either <see cref="Connection"/>, if specified, or
    /// the environment variable specified by <see cref="ConnectionFromEnv"/>.
    /// </summary>
    /// <value>The Azure Storage connection string if specified; otherwise <see langword="null"/>.</value>
    public string? ConnectionString
    {
        get
        {
            _connectionString ??= new Lazy<string?>(ResolveConnectionString, LazyThreadSafetyMode.None);
            return _connectionString.Value;
        }
    }

    /// <summary>
    /// Gets the optional Azure Storage host suffix for private clouds.
    /// </summary>
    /// <remarks>
    /// This value may only be specified when the value of <see cref="Cloud"/> is <c>"private"</c>.
    /// </remarks>
    /// <value>The private cloud's suffix for Azure Storage endpoints.</value>
    public string? EndpointSuffix { get; set; }

    /// <summary>
    /// Gets the maximum number of activity work items that a single worker may process at any time.
    /// </summary>
    /// <value>The positive number of work items.</value>
    [Range(1, int.MaxValue)]
    public int MaxActivitiesPerWorker { get; set; } = 10;

    /// <summary>
    /// Gets the maximum number of orchestration work items that a single worker may process at any time.
    /// </summary>
    /// <value>The positive number of work items.</value>
    [Range(1, int.MaxValue)]
    public int MaxOrchestrationsPerWorker { get; set; } = 5;

    /// <summary>
    /// Gets the name of the configured task hub present in Azure Storage.
    /// </summary>
    /// <remarks>If unspecified, the default name is <c>"TestHubName"</c>.</remarks>
    /// <value>The name of the task hub.</value>
    [Required]
    public string TaskHubName { get; set; } = "TestHubName";

    /// <summary>
    /// Gets a value indicating whether a managed identity should be used to authenticate the connection to Azure Storage.
    /// </summary>
    /// <remarks>
    /// Managed identities cannot be used with connection strings.
    /// </remarks>
    /// <value>
    /// <see langword="true"/> if a managed identity is available in the service pod and should be used to authenticate;
    /// otherwise, <see langword="false"/>.
    /// </value>
    [Obsolete("Use UseWorkloadIdentity instead.")]
    public bool UseManagedIdentity { get; set; }

    /// <summary>
    /// Gets a value indicating whether a federated identity should be used to authenticate the connection to Azure Storage.
    /// </summary>
    /// <remarks>
    /// Workload identity cannot be used with connection strings.
    /// </remarks>
    /// <value>
    /// <see langword="true"/> if a federated identity is available in the service pod and should be used to authenticate;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool UseWorkloadIdentity { get; set; }

    private Lazy<string?>? _connectionString;

    private string? ResolveConnectionString()
    {
        return Connection is null
            ? Environment.GetEnvironmentVariable(ConnectionFromEnv ?? DefaultConnectionEnvironmentVariable, EnvironmentVariableTarget.Process)
            : Connection;
    }

    IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);
        return ValidateCloudMetadata().Concat(AccountName is null ? ValidateConnectionStringMetadata() : ValidateAccountMetadata());
    }

    private IEnumerable<ValidationResult> ValidateCloudMetadata()
    {
        if (CloudEnvironment is CloudEnvironment.Private)
        {
            if (ActiveDirectoryEndpoint is null)
                yield return new ValidationResult(SR.MissingPrivateCloudValue, [nameof(ActiveDirectoryEndpoint)]);

            if (EndpointSuffix is null)
                yield return new ValidationResult(SR.MissingPrivateCloudValue, [nameof(EndpointSuffix)]);

            if (EndpointSuffix is not null && string.IsNullOrWhiteSpace(EndpointSuffix))
                yield return new ValidationResult(SR.EmptyOrWhiteSpace, [nameof(EndpointSuffix)]);
        }
        else
        {
            if (ActiveDirectoryEndpoint is not null)
                yield return new ValidationResult(SR.PrivateCloudOnlyValue, [nameof(ActiveDirectoryEndpoint)]);

            if (EndpointSuffix is not null)
                yield return new ValidationResult(SR.PrivateCloudOnlyValue, [nameof(EndpointSuffix)]);
        }
    }

    private IEnumerable<ValidationResult> ValidateAccountMetadata()
    {
        if (string.IsNullOrWhiteSpace(AccountName))
            yield return new ValidationResult(SR.EmptyOrWhiteSpace, [nameof(AccountName)]);

        if (CloudEnvironment is CloudEnvironment.Unknown)
            yield return new ValidationResult(SRF.Format(SRF.UnknownValueFormat, Cloud), [nameof(Cloud)]);

        if (Connection is not null)
            yield return new ValidationResult(SR.AmbiguousConnection, [nameof(AccountName), nameof(Connection)]);

        if (ConnectionFromEnv is not null)
            yield return new ValidationResult(SR.AmbiguousConnection, [nameof(AccountName), nameof(ConnectionFromEnv)]);

#pragma warning disable CS0618 // Type or member is obsolete
        if (!UseManagedIdentity && !UseWorkloadIdentity && ClientId is not null)
            yield return new ValidationResult(SR.MissingCredentialOption, [nameof(ClientId)]);
#pragma warning restore CS0618 // Type or member is obsolete

#pragma warning disable CS0618 // Type or member is obsolete
        if (UseManagedIdentity && UseWorkloadIdentity)
            yield return new ValidationResult(SR.AmbiguousCredential, [nameof(UseManagedIdentity), nameof(UseWorkloadIdentity)]);
#pragma warning restore CS0618 // Type or member is obsolete
    }

    private IEnumerable<ValidationResult> ValidateConnectionStringMetadata()
    {
        if (ClientId is not null)
            yield return new ValidationResult(SR.ServiceUriOnlyValue, [nameof(ClientId)]);

        if (Cloud is not null)
            yield return new ValidationResult(SR.ServiceUriOnlyValue, [nameof(Cloud)]);

#pragma warning disable CS0618 // Type or member is obsolete
        if (UseManagedIdentity)
            yield return new ValidationResult(SR.ServiceUriOnlyValue, [nameof(UseManagedIdentity)]);
#pragma warning restore CS0618 // Type or member is obsolete

        if (UseWorkloadIdentity)
            yield return new ValidationResult(SR.ServiceUriOnlyValue, [nameof(UseWorkloadIdentity)]);

        if (Connection is not null && string.IsNullOrWhiteSpace(Connection))
            yield return new ValidationResult(SR.EmptyOrWhiteSpace, [nameof(Connection)]);
        else if (ConnectionFromEnv is not null && string.IsNullOrWhiteSpace(ConnectionFromEnv))
            yield return new ValidationResult(SR.EmptyOrWhiteSpace, [nameof(ConnectionFromEnv)]);
        else if (Connection is not null && ConnectionFromEnv is not null)
            yield return new ValidationResult(SR.AmbiguousConnection, [nameof(Connection), nameof(ConnectionFromEnv)]);
        else if (string.IsNullOrWhiteSpace(ConnectionString))
            yield return new ValidationResult(SRF.Format(SRF.InvalidConnectionEnvironmentVariableFormat, ConnectionFromEnv ?? DefaultConnectionEnvironmentVariable), [nameof(ConnectionFromEnv)]);
    }
}
