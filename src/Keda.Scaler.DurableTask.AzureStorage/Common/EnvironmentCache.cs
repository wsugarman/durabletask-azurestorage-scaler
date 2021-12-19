// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;

namespace Keda.Scaler.DurableTask.AzureStorage.Common
{
    internal sealed class EnvironmentCache : IEnvironment
    {
        private readonly IEnvironment _environment;
        private readonly ConcurrentDictionary<(string, EnvironmentVariableTarget), string?> _cache = new ConcurrentDictionary<(string, EnvironmentVariableTarget), string?>();

        public EnvironmentCache(IEnvironment environment)
            => _environment = environment ?? throw new ArgumentNullException(nameof(environment));

        /// <inheritdoc/>
        public string? GetEnvironmentVariable(string variable, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
        {
            if (!_cache.TryGetValue((variable, target), out string? value))
            {
                value = _environment.GetEnvironmentVariable(variable, target);
                _cache.TryAdd((variable, target), value);
            }

            return value;
        }
    }
}
