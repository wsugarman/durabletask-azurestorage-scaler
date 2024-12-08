// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Keda.Scaler.DurableTask.AzureStorage.Clients;
using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.Metadata;

internal sealed class ValidateScalerOptions : IValidateOptions<ScalerOptions>
{
    public ValidateOptionsResult Validate(string? name, ScalerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = options.AccountName is null
            ? ValidateStringBasedConnection(options)
            : ValidateUriBasedConnection(options);

        return failures.Count is 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static List<string> ValidateStringBasedConnection(ScalerOptions options)
    {
        List<string> failures = [];

        // Check properties that are specific to URI-based connections
        if (options.ClientId is not null)
            failures.Add(SRF.Format(SRF.ServiceUriOnlyProperty, nameof(ScalerOptions.ClientId)));

        if (options.Cloud is not null)
            failures.Add(SRF.Format(SRF.ServiceUriOnlyProperty, nameof(ScalerOptions.Cloud)));

        if (options.EndpointSuffix is not null)
            failures.Add(SRF.Format(SRF.ServiceUriOnlyProperty, nameof(ScalerOptions.EndpointSuffix)));

        if (options.EntraEndpoint is not null)
            failures.Add(SRF.Format(SRF.ServiceUriOnlyProperty, nameof(ScalerOptions.EntraEndpoint)));

        if (options.UseManagedIdentity)
            failures.Add(SRF.Format(SRF.ServiceUriOnlyProperty, nameof(ScalerOptions.UseManagedIdentity)));

        // Validate the connection string properties
        if (options.Connection is not null && string.IsNullOrWhiteSpace(options.Connection))
            failures.Add(SRF.Format(SRF.EmptyOrWhiteSpace, nameof(ScalerOptions.Connection)));
        else if (options.ConnectionFromEnv is not null && string.IsNullOrWhiteSpace(options.ConnectionFromEnv))
            failures.Add(SRF.Format(SRF.EmptyOrWhiteSpace, nameof(ScalerOptions.ConnectionFromEnv)));
        else if (options.Connection is not null && options.ConnectionFromEnv is not null)
            failures.Add(SR.AmbiguousConnection);

        return failures;
    }

    private static List<string> ValidateUriBasedConnection(ScalerOptions options)
    {
        List<string> failures = [];

        // Check properties that are specific to connection strings
        if (options.Connection is not null || options.ConnectionFromEnv is not null)
            failures.Add(SR.AmbiguousConnection);

        // Validate the account properties
        if (string.IsNullOrWhiteSpace(options.AccountName))
            failures.Add(SRF.Format(SRF.EmptyOrWhiteSpace, nameof(ScalerOptions.AccountName)));

        if (!options.UseManagedIdentity)
            failures.Add(SR.MissingManagedIdentity);

        if (!AzureCloudEndpoints.TryParseEnvironment(options.Cloud, out CloudEnvironment cloud))
            failures.Add(SRF.Format(SRF.UnknownCloudValue, options.Cloud));

        // Validate private cloud properties
        if (cloud is CloudEnvironment.Private)
        {
            if (string.IsNullOrWhiteSpace(options.EndpointSuffix))
                failures.Add(SRF.Format(SRF.MissingPrivateCloudProperty, nameof(ScalerOptions.EndpointSuffix)));

            if (options.EntraEndpoint is null)
                failures.Add(SRF.Format(SRF.MissingPrivateCloudProperty, nameof(ScalerOptions.EntraEndpoint)));
        }
        else
        {
            if (options.EndpointSuffix is not null)
                failures.Add(SRF.Format(SRF.PrivateCloudOnlyProperty, nameof(ScalerOptions.EndpointSuffix)));

            if (options.EntraEndpoint is not null)
                failures.Add(SRF.Format(SRF.PrivateCloudOnlyProperty, nameof(ScalerOptions.EntraEndpoint)));
        }

        return failures;
    }
}
