// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Keda.Scaler.DurableTask.AzureStorage.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Keda.Scaler.DurableTask.AzureStorage;

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
    public string? AccountName { get; init; }

    /// <summary>
    /// Gets the optional URL from which tokens may be requested for private clouds.
    /// </summary>
    /// <remarks>
    /// This value may only be specified when the value of <see cref="Cloud"/> is <c>"private"</c>.
    /// </remarks>
    /// <value>The private cloud's host authority for Azure Active Directory (AAD).</value>
    public Uri? ActiveDirectoryEndpoint { get; init; }

    /// <summary>
    /// Gets the optional client id to be used when authenticating with managed identity.
    /// </summary>
    /// <remarks>
    /// This value is only required if <see cref="UseManagedIdentity"/> is <see langword="true"/>.
    /// If managed identity is specified, but the <see cref="ClientId"/> is left unspecified, then a default
    /// identity is chosen. Be sure to specify a client id if there are multiple identities.
    /// </remarks>
    /// <value>The client id to be used with managed identity.</value>
    public string? ClientId { get; init; }

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
    public string? Cloud { get; init; }

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
    public string? Connection { get; init; }

    /// <summary>
    /// Gets the optional name of the environment variable whose value is the connection string
    /// for the Azure Storage account used by the Durable Task framework.
    /// </summary>
    /// <remarks>
    /// This value is only required if <see cref="AccountName"/> and <see cref="Connection"/>
    /// are not specified.
    /// </remarks>
    /// <value>The name of an environment variable in the deployment if specified; otherwise, <see langword="null"/>.</value>
    public string? ConnectionFromEnv { get; init; }

    /// <summary>
    /// Gets the optional Azure Storage host suffix for private clouds.
    /// </summary>
    /// <remarks>
    /// This value may only be specified when the value of <see cref="Cloud"/> is <c>"private"</c>.
    /// </remarks>
    /// <value>The private cloud's suffix for Azure Storage endpoints.</value>
    public string? EndpointSuffix { get; init; }

    /// <summary>
    /// Gets the maximum number of activity work items that a single worker may process at any time.
    /// </summary>
    /// <value>The positive number of work items.</value>
    [Range(1, int.MaxValue)]
    public int MaxActivitiesPerWorker { get; init; } = 10;

    /// <summary>
    /// Gets the maximum number of orchestration work items that a single worker may process at any time.
    /// </summary>
    /// <value>The positive number of work items.</value>
    [Range(1, int.MaxValue)]
    public int MaxOrchestrationsPerWorker { get; init; } = 5;

    /// <summary>
    /// Gets the name of the configured task hub present in Azure Storage.
    /// </summary>
    /// <remarks>If unspecified, the default name is <c>"TestHubName"</c>.</remarks>
    /// <value>The name of the task hub.</value>
    [Required]
    public string TaskHubName { get; init; } = "TestHubName";

    /// <summary>
    /// Gets a value indicating whether a managed identity should be used to authenticate the connection to Azure Storage.
    /// </summary>
    /// <remarks>
    /// <para>
    /// External scalers do not support Trigger Authentication, and either AAD Pod Identity or Workload Identity
    /// must be installed in the Kubernetes cluster with the appropriate annotations, bindings, and/or labels.
    /// </para>
    /// <para>
    /// If <see langword="true"/> then <see cref="AccountName"/> must also be specified.
    /// </para>
    /// </remarks>
    /// <value>
    /// <see langword="true"/> if a managed identity is available in the service pod and should be used to authenticate;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool UseManagedIdentity { get; init; }

    /// <summary>
    /// Gets the resolved connection string based on the <see cref="Connection"/> and <see cref="ConnectionFromEnv"/>.
    /// </summary>
    /// <param name="environment">The <see cref="IProcessEnvironment"/> containing any variables.</param>
    /// <returns>
    /// <see cref="Connection"/>, if specified; otherwise, the value of an environment variable specified by
    /// <see cref="ConnectionFromEnv"/> or <c>AzureWebJobsStorage</c> by default.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="environment"/> is <see langword="null"/>.</exception>
    public string? ResolveConnectionString(IProcessEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);

        return Connection is null
            ? environment.GetVariable(ConnectionFromEnv ?? DefaultConnectionEnvironmentVariable)
            : Connection;
    }

    /// <summary>
    /// Enumerates of the errors associated with the state of the <see cref="ScalerMetadata"/>, if any,
    /// based on a combination of members.
    /// </summary>
    /// <remarks>
    /// <see cref="Validate(ValidationContext)"/> does not return all possible errors with this instance.
    /// Instead, users should use the <see cref="Validator"/> class to determine if an object is valid.
    /// </remarks>
    /// <param name="validationContext">The context for the validation.</param>
    /// <returns>Zero or more errors based on the state of this instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="validationContext"/> is <see langword="null"/>.</exception>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);
        return ValidateCloudMetadata()
            .Concat(AccountName is null
                ? ValidateConnectionStringMetadata(validationContext)
                : ValidateAccountMetadata());
    }

    private IEnumerable<ValidationResult> ValidateCloudMetadata()
    {
        if (CloudEnvironment is CloudEnvironment.Private)
        {
            if (ActiveDirectoryEndpoint is null)
                yield return new ValidationResult(SR.Format(SR.PrivateCloudRequiredFieldFormat, nameof(ActiveDirectoryEndpoint)));

            if (EndpointSuffix is null)
                yield return new ValidationResult(SR.Format(SR.PrivateCloudRequiredFieldFormat, nameof(EndpointSuffix)));

            if (EndpointSuffix is not null && string.IsNullOrWhiteSpace(EndpointSuffix))
                yield return new ValidationResult(SR.Format(SR.OptionalBlankValueFormat, nameof(EndpointSuffix)));
        }
        else
        {
            if (ActiveDirectoryEndpoint is not null)
                yield return new ValidationResult(SR.Format(SR.PrivateCloudOnlyFieldFormat, nameof(ActiveDirectoryEndpoint)));

            if (EndpointSuffix is not null)
                yield return new ValidationResult(SR.Format(SR.PrivateCloudOnlyFieldFormat, nameof(EndpointSuffix)));
        }
    }

    private IEnumerable<ValidationResult> ValidateAccountMetadata()
    {
        if (string.IsNullOrWhiteSpace(AccountName))
            yield return new ValidationResult(SR.Format(SR.OptionalBlankValueFormat, nameof(AccountName)));

        if (CloudEnvironment is CloudEnvironment.Unknown)
            yield return new ValidationResult(SR.Format(SR.UnknownValueFormat, Cloud, nameof(Cloud)));

        if (Connection is not null)
            yield return new ValidationResult(SR.Format(SR.AmbiguousConnectionOptionFormat, nameof(Connection)));

        if (ConnectionFromEnv is not null)
            yield return new ValidationResult(SR.Format(SR.AmbiguousConnectionOptionFormat, nameof(ConnectionFromEnv)));

        if (!UseManagedIdentity && ClientId is not null)
            yield return new ValidationResult(SR.Format(SR.MissingIdentityCredentialOptionFormat, nameof(ClientId)));
    }

    private IEnumerable<ValidationResult> ValidateConnectionStringMetadata(ValidationContext validationContext)
    {
        if (ClientId is not null)
            yield return new ValidationResult(SR.Format(SR.InvalidConnectionStringOptionFormat, nameof(ClientId)));

        if (Cloud is not null)
            yield return new ValidationResult(SR.Format(SR.InvalidConnectionStringOptionFormat, nameof(Cloud)));

        if (UseManagedIdentity)
            yield return new ValidationResult(SR.Format(SR.InvalidConnectionStringOptionFormat, nameof(UseManagedIdentity)));

        IProcessEnvironment environment = validationContext.GetRequiredService<IProcessEnvironment>();
        if (Connection is not null && string.IsNullOrWhiteSpace(Connection))
            yield return new ValidationResult(SR.Format(SR.OptionalBlankValueFormat, nameof(Connection)));
        else if (ConnectionFromEnv is not null && string.IsNullOrWhiteSpace(ConnectionFromEnv))
            yield return new ValidationResult(SR.Format(SR.OptionalBlankValueFormat, nameof(ConnectionFromEnv)));
        else if (string.IsNullOrWhiteSpace(ResolveConnectionString(environment)))
            yield return new ValidationResult(SR.Format(SR.BlankConnectionVarFormat, ConnectionFromEnv ?? DefaultConnectionEnvironmentVariable));
    }
}
