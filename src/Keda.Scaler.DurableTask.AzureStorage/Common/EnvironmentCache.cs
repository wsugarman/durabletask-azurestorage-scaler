// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;

namespace Keda.Scaler.DurableTask.AzureStorage.Common;

internal sealed class EnvironmentCache(IProcessEnvironment environment) : IProcessEnvironment
{
    private readonly IProcessEnvironment _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    private readonly ConcurrentDictionary<string, string?> _cache = new();

    /// <inheritdoc/>
    public string? GetVariable(string variable)
        => _cache.GetOrAdd(variable, _ => _environment.GetVariable(variable));
}
