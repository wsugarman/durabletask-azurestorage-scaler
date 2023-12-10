// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;

namespace Keda.Scaler.DurableTask.AzureStorage.Common;

/// <summary>
/// Represents an environment that defines any number of variables for the current process.
/// </summary>
public interface IProcessEnvironment
{
    /// <inheritdoc cref="Environment.GetEnvironmentVariable(string)"/>
    string? GetVariable(string variable);
}
