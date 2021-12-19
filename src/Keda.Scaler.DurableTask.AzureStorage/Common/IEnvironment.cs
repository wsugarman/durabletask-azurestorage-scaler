// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;

namespace Keda.Scaler.DurableTask.AzureStorage.Common
{
    /// <summary>
    /// Represents an environment that defines any number of variables.
    /// </summary>
    public interface IEnvironment
    {
        /// <inheritdoc cref="Environment.GetEnvironmentVariable(string, EnvironmentVariableTarget)"/>
        string? GetEnvironmentVariable(string variable, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process);
    }
}
