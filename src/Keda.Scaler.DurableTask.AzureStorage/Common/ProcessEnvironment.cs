// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;

namespace Keda.Scaler.DurableTask.AzureStorage.Common;

/// <summary>
/// Represents a collection of utilities related to the process environment.
/// </summary>
public static class ProcessEnvironment
{
    /// <summary>
    /// Gets the <see cref="IProcessEnvironment"/> instance for the current environment.
    /// </summary>
    /// <value>The current environment.</value>
    public static IProcessEnvironment Current { get; } = new CurrentEnvironment();

    private sealed class CurrentEnvironment : IProcessEnvironment
    {
        public string? GetEnvironmentVariable(string variable)
            => Environment.GetEnvironmentVariable(variable);
    }
}
