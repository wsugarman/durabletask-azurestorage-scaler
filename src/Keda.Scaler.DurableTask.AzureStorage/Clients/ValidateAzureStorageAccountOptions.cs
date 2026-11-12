// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.Clients;

internal sealed class ValidateAzureStorageAccountOptions : IValidateOptions<AzureStorageAccountOptions>
{
    public ValidateOptionsResult Validate(string? name, AzureStorageAccountOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.AccountName is null && string.IsNullOrWhiteSpace(options.ConnectionString))
            return ValidateOptionsResult.Fail(SR.UnresolvedConnectionString);

        return ValidateOptionsResult.Success;
    }
}
