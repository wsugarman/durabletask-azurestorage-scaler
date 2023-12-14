// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;

namespace Keda.Scaler.DurableTask.AzureStorage.Test;

internal static class TestEnvironment
{
    public static IDisposable SetVariable(string key, string? value)
    {
        string? previous = Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable(key, value, EnvironmentVariableTarget.Process);
        return new VariableReplacement(key, previous);
    }

    private sealed class VariableReplacement(string key, string? value) : IDisposable
    {
        public string Key { get; } = key ?? throw new ArgumentNullException(nameof(key));

        public string? Value { get; } = value;

        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                Environment.SetEnvironmentVariable(Key, Value, EnvironmentVariableTarget.Process);
                _disposed = true;
            }
        }
    }
}
