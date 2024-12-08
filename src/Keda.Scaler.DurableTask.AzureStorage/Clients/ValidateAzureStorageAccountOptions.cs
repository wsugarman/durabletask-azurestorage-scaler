// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.Metadata;
using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.Clients;

internal sealed class ValidateAzureStorageAccountOptions(IOptionsSnapshot<ScalerOptions> scalerOptions) : IValidateOptions<AzureStorageAccountOptions>
{
    private readonly ScalerOptions _scalerOptions = scalerOptions?.Get(default) ?? throw new ArgumentNullException(nameof(scalerOptions));

    public ValidateOptionsResult Validate(string? name, AzureStorageAccountOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.AccountName is null && string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            string failure = SRF.Format(SRF.InvalidConnectionEnvironmentVariable, _scalerOptions.ConnectionFromEnv ?? AzureStorageAccountOptions.DefaultConnectionEnvironmentVariable);
            return ValidateOptionsResult.Fail(failure);
        }

        return ValidateOptionsResult.Success;
    }
}
