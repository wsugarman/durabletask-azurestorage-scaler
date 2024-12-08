// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Keda.Scaler.DurableTask.AzureStorage.Metadata;
using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.Clients;

internal sealed class ValidateAzureStorageAccountOptions(IOptionsSnapshot<ScalerOptions> scalerOptions) : IValidateOptions<AzureStorageAccountOptions>
{
    private readonly ScalerOptions _scalerOptions = scalerOptions?.Get(default) ?? throw new ArgumentNullException(nameof(scalerOptions));

    public ValidateOptionsResult Validate(string? name, AzureStorageAccountOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        List<string> failures = options.AccountName is null
            ? ValidateStringBasedConnection(options)
            : ValidateUriBasedConnection(options);

        return failures.Count is 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private List<string> ValidateStringBasedConnection(AzureStorageAccountOptions options)
    {
        List<string> failures = [];

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
            failures.Add(SRF.Format(SRF.InvalidConnectionEnvironmentVariable, _scalerOptions.ConnectionFromEnv ?? AzureStorageAccountOptions.DefaultConnectionEnvironmentVariable));

        return failures;
    }

    private List<string> ValidateUriBasedConnection(AzureStorageAccountOptions options)
    {
        List<string> failures = [];

        // Note: EndpointSuffix can only be null for invalid clouds at this point
        if (string.IsNullOrWhiteSpace(options.EndpointSuffix))
            failures.Add(SRF.Format(SRF.UnknownCloudValue, _scalerOptions.Cloud));

        return failures;
    }
}
