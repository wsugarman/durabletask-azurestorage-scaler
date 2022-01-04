// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Collections.Generic;
using Keda.Scaler.DurableTask.AzureStorage.Common;

namespace Keda.Scaler.DurableTask.AzureStorage.Test;

internal sealed class MockEnvironment : IProcessEnvironment
{
    private readonly Dictionary<string, string> _env = new Dictionary<string, string>();

    public string? GetEnvironmentVariable(string variable)
        => _env.TryGetValue(variable, out string? value) ? value : null;

    public void SetEnvironmentVariable(string variable, string? value)
    {
        if (value is null)
            _env.Remove(variable);
        else
            _env[variable] = value;
    }
}
