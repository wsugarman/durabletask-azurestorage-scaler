// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Keda.Scaler.DurableTask.AzureStorage.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Keda.Scaler.DurableTask.AzureStorage;

/// <summary>
/// Represents the metadata present in the KEDA ScaledObject resource specified to the
/// Durable Task Azure Storage external scaler.
/// </summary>
public sealed class ScalerMetadata : IValidatableObject
{
    public const string DefaultConnectionEnvironmentVariable = "AzureWebJobsStorage";

    // TODO: Add support for Private clouds with AAD Pod Identity

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
    /// Gets the cloud environment that contains the Azure Storage account used by the Durable Task framework.
    /// </summary>
    /// <value>
    /// The Azure cloud environment containing the storage account, if a known value;
    /// otherwise, <see cref="CloudEnvironment.Unknown"/>.
    /// </value>
    public CloudEnvironment CloudEnvironment => Cloud switch
    {
        nameof(CloudEnvironment.AzurePublicCloud) or null => CloudEnvironment.AzurePublicCloud,
        nameof(CloudEnvironment.AzureUSGovernmentCloud) => CloudEnvironment.AzureUSGovernmentCloud,
        nameof(CloudEnvironment.AzureChinaCloud) => CloudEnvironment.AzureChinaCloud,
        nameof(CloudEnvironment.AzureGermanCloud) => CloudEnvironment.AzureGermanCloud,
        _ => CloudEnvironment.Unknown,
    };

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
    /// Gets the ratio by which the replicas are increased or decreased.
    /// </summary>
    /// <remarks>
    /// For example, if the value is <c>2</c>, then the replica count is doubled when scaling up
    /// and halved when scaling down.
    /// </remarks>
    /// <value>The ratio by which the replica count is multiplied when scaling up and down.</value>
    public double ScaleIncrement { get; init; } = 1.5;

    /// <summary>
    /// Gets the desired length of time in milliseconds orchestrations, activities, and actors take to receive messages.
    /// </summary>
    /// <remarks>Durable Task framework does not support a value larger than 1000 milliseconds or 1 second.</remarks>
    /// <value>The desired length of time messages sit in their respective queues before being processed.</value>
    public int MaxMessageLatencyMilliseconds { get; init; } = 1000;

    /// <summary>
    /// Gets the name of the configured task hub present in Azure Storage.
    /// </summary>
    /// <remarks>If unspecified, the default name is <c>"TestHubName"</c>.</remarks>
    /// <value>The name of the task hub.</value>
    public string TaskHubName { get; init; } = "TestHubName";

    /// <summary>
    /// Gets a value indicating whether AAD Pod identity should be used to authenticate the connection to Azure Storage.
    /// </summary>
    /// <remarks>
    /// <para>
    /// External scalers do not yet support Trigger Authentication.
    /// </para>
    /// <para>
    /// If <see langword="true"/> then <see cref="AccountName"/> must also be specified.
    /// </para>
    /// </remarks>
    /// <value>
    /// <see langword="true"/> if AAD pod identity is present in the service pod and should be used to authenticate;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool UseAAdPodIdentity { get; init; }

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
        if (environment is null)
            throw new ArgumentNullException(nameof(environment));

        return Connection is null
            ? environment.GetEnvironmentVariable(ConnectionFromEnv ?? DefaultConnectionEnvironmentVariable)
            : Connection;
    }

    /// <summary>
    /// Enumerates all of the errors associated with the state of the <see cref="ScalerMetadata"/>, if any.
    /// </summary>
    /// <param name="validationContext">The context for the validation.</param>
    /// <returns>Zero or more errors based on the state of this instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="validationContext"/> is <see langword="null"/>.</exception>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (validationContext is null)
            throw new ArgumentNullException(nameof(validationContext));

        if (MaxMessageLatencyMilliseconds < 0)
            yield return new ValidationResult(SR.Format(SR.NegativeValueFormat, nameof(MaxMessageLatencyMilliseconds)));

        if (MaxMessageLatencyMilliseconds > 1000)
            yield return new ValidationResult(SR.Format(SR.ValueTooBigFormat, nameof(MaxMessageLatencyMilliseconds), 1000));

        if (ScaleIncrement <= 1)
            yield return new ValidationResult(SR.Format(SR.ValueTooSmallFormat, nameof(ScaleIncrement), 1));

        if (string.IsNullOrWhiteSpace(TaskHubName))
            yield return new ValidationResult(SR.Format(SR.RequiredBlankValueFormat, nameof(TaskHubName)));

        if (UseAAdPodIdentity)
        {
            if (AccountName is null)
                yield return new ValidationResult(SR.Format(SR.AadRequiredFieldFormat, nameof(AccountName)));

            if (AccountName is not null && string.IsNullOrWhiteSpace(AccountName))
                yield return new ValidationResult(SR.Format(SR.OptionalBlankValueFormat, nameof(AccountName)));

            if (CloudEnvironment == CloudEnvironment.Unknown)
                yield return new ValidationResult(SR.Format(SR.UnknownValueFormat, Cloud, nameof(Cloud)));

            if (Connection is not null)
                yield return new ValidationResult(SR.Format(SR.InvalidAadFieldFormat, nameof(Connection)));

            if (ConnectionFromEnv is not null)
                yield return new ValidationResult(SR.Format(SR.InvalidAadFieldFormat, nameof(ConnectionFromEnv)));
        }
        else
        {
            if (AccountName is not null)
                yield return new ValidationResult(SR.Format(SR.AadOnlyFieldFormat, nameof(AccountName)));

            if (Cloud is not null)
                yield return new ValidationResult(SR.Format(SR.AadOnlyFieldFormat, nameof(Cloud)));

            IProcessEnvironment environment = validationContext.GetRequiredService<IProcessEnvironment>();
            if (Connection is not null && string.IsNullOrWhiteSpace(Connection))
                yield return new ValidationResult(SR.Format(SR.OptionalBlankValueFormat, nameof(Connection)));
            else if (ConnectionFromEnv is not null && string.IsNullOrWhiteSpace(ConnectionFromEnv))
                yield return new ValidationResult(SR.Format(SR.OptionalBlankValueFormat, nameof(ConnectionFromEnv)));
            else if (string.IsNullOrWhiteSpace(ResolveConnectionString(environment)))
                yield return new ValidationResult(SR.Format(SR.BlankConnectionVarFormat, ConnectionFromEnv ?? DefaultConnectionEnvironmentVariable));
        }
    }
}
