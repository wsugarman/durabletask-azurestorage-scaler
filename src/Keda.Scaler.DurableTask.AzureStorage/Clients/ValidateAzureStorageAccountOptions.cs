// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Keda.Scaler.DurableTask.AzureStorage.Clouds;
using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.Clients;

internal sealed class ValidateAzureStorageAccountOptions(IScalerMetadataAccessor accessor) : IValidateOptions<AzureStorageAccountOptions>
{
    private readonly IScalerMetadataAccessor _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));

    public ValidateOptionsResult Validate(string? name, AzureStorageAccountOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ScalerMetadata metadata = _accessor.ScalerMetadata ?? throw new InvalidOperationException(SR.ScalerMetadataNotFound);

        List<string> failures = options.AccountName is null ? ValidateStringBasedConnection(metadata, options) : ValidateUriBasedConnection(metadata, options);
        return failures.Count is 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static List<string> ValidateStringBasedConnection(ScalerMetadata metadata, AzureStorageAccountOptions options)
    {
        List<string> failures = [];

        // Check properties that are specific to URI-based connections
        if (metadata.ClientId is not null)
            failures.Add(SRF.Format(SRF.ServiceUriOnlyProperty, nameof(ScalerMetadata.ClientId)));

        if (metadata.Cloud is not null)
            failures.Add(SRF.Format(SRF.ServiceUriOnlyProperty, nameof(ScalerMetadata.Cloud)));

        if (metadata.EndpointSuffix is not null)
            failures.Add(SRF.Format(SRF.ServiceUriOnlyProperty, nameof(ScalerMetadata.EndpointSuffix)));

        if (metadata.EntraEndpoint is not null)
            failures.Add(SRF.Format(SRF.ServiceUriOnlyProperty, nameof(ScalerMetadata.EntraEndpoint)));

        if (metadata.UseManagedIdentity)
            failures.Add(SRF.Format(SRF.ServiceUriOnlyProperty, nameof(ScalerMetadata.UseManagedIdentity)));

        // Validate the connection string properties
        if (metadata.Connection is not null && string.IsNullOrWhiteSpace(metadata.Connection))
            failures.Add(SRF.Format(SRF.EmptyOrWhiteSpace, nameof(ScalerMetadata.Connection)));
        else if (metadata.ConnectionFromEnv is not null && string.IsNullOrWhiteSpace(metadata.ConnectionFromEnv))
            failures.Add(SRF.Format(SRF.EmptyOrWhiteSpace, nameof(ScalerMetadata.ConnectionFromEnv)));
        else if (metadata.Connection is not null && metadata.ConnectionFromEnv is not null)
            failures.Add(SR.AmbiguousConnection);
        else if (string.IsNullOrWhiteSpace(options.ConnectionString))
            failures.Add(SRF.Format(SRF.InvalidConnectionEnvironmentVariable, metadata.ConnectionFromEnv ?? AzureStorageAccountOptions.DefaultConnectionEnvironmentVariable));

        return failures;
    }

    private static List<string> ValidateUriBasedConnection(ScalerMetadata metadata, AzureStorageAccountOptions options)
    {
        List<string> failures = [];

        // Check properties that are specific to connection strings
        if (metadata.Connection is not null || metadata.ConnectionFromEnv is not null)
            failures.Add(SR.AmbiguousConnection);

        // Validate private cloud properties
        if (string.Equals(metadata.Cloud, CloudEnvironment.Private, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(metadata.EndpointSuffix))
                failures.Add(SRF.Format(SRF.MissingPrivateCloudProperty, metadata.EndpointSuffix));

            if (metadata.EntraEndpoint is null)
                failures.Add(SRF.Format(SRF.MissingPrivateCloudProperty, metadata.EntraEndpoint));
        }
        else
        {
            if (metadata.EndpointSuffix is not null)
                failures.Add(SRF.Format(SRF.PrivateCloudOnlyProperty, metadata.EndpointSuffix));

            if (metadata.EntraEndpoint is not null)
                failures.Add(SRF.Format(SRF.PrivateCloudOnlyProperty, metadata.EntraEndpoint));
        }

        // Validate the account properties
        if (string.IsNullOrWhiteSpace(options.AccountName))
            failures.Add(SRF.Format(SRF.EmptyOrWhiteSpace, nameof(ScalerMetadata.AccountName)));

        if (!metadata.UseManagedIdentity)
        {
            if (metadata.ClientId is not null)
                failures.Add(SRF.Format(SRF.IdentityConnectionOnlyProperty, metadata.ClientId));

            if (metadata.EntraEndpoint is not null)
                failures.Add(SRF.Format(SRF.IdentityConnectionOnlyProperty, metadata.EntraEndpoint));
        }

        // Note: EndpointSuffix can only be null for invalid clouds, as we checked the private cloud above
        if (string.IsNullOrWhiteSpace(options.EndpointSuffix))
            failures.Add(SRF.Format(SRF.UnknownCloudValue, metadata.Cloud));

        return failures;
    }
}
