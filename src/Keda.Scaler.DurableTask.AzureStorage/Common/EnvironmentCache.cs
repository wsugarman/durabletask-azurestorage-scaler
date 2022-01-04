// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;

namespace Keda.Scaler.DurableTask.AzureStorage.Common;

internal sealed class EnvironmentCache : IProcessEnvironment
{
    private readonly IProcessEnvironment _environment;
    private readonly ConcurrentDictionary<string, string?> _cache = new ConcurrentDictionary<string, string?>();

    public EnvironmentCache(IProcessEnvironment environment)
        => _environment = environment ?? throw new ArgumentNullException(nameof(environment));

    /// <inheritdoc/>
    public string? GetEnvironmentVariable(string variable)
    {
        if (!_cache.TryGetValue(variable, out string? value))
        {
            value = _environment.GetEnvironmentVariable(variable);
            _cache.TryAdd(variable, value);
        }

        return value;
    }
}
