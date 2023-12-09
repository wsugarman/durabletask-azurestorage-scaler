// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using Keda.Scaler.DurableTask.AzureStorage.Common;

namespace Keda.Scaler.DurableTask.AzureStorage.Test;

internal sealed class MockEnvironment : IProcessEnvironment
{
    private readonly ConcurrentDictionary<string, string> _env = [];

    public string? GetEnvironmentVariable(string variable)
        => _env.TryGetValue(variable, out string? value) ? value : null;

    public void SetEnvironmentVariable(string variable, string? value)
    {
        if (value is null)
            _ = _env.TryRemove(variable, out _);
        else
            _env[variable] = value;
    }
}
