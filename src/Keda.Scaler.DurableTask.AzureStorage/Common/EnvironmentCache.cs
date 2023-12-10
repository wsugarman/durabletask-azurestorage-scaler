// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;

namespace Keda.Scaler.DurableTask.AzureStorage.Common;

internal sealed class EnvironmentCache : IProcessEnvironment
{
    private readonly IProcessEnvironment _environment;
    private readonly ConcurrentDictionary<string, string?> _cache = new();

    public EnvironmentCache(IProcessEnvironment environment)
        => _environment = environment ?? throw new ArgumentNullException(nameof(environment));

    /// <inheritdoc/>
    public string? GetVariable(string variable)
        => _cache.GetOrAdd(variable, _ => _environment.GetVariable(variable));
}
