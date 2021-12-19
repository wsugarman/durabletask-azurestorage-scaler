// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;

namespace Keda.Scaler.DurableTask.AzureStorage.Common
{
    /// <summary>
    /// Represents the current environment encapsulating this process. This class cannot be inherited.
    /// </summary>
    public sealed class CurrentEnvironment : IEnvironment
    {
        /// <summary>
        /// Gets the <see cref="IEnvironment"/> instance for the current environment.
        /// </summary>
        /// <value>The current environment.</value>
        public static IEnvironment Instance { get; } = new CurrentEnvironment();

        private CurrentEnvironment()
        { }

        /// <inheritdoc/>
        public string? GetEnvironmentVariable(string variable, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
            => Environment.GetEnvironmentVariable(variable, target);
    }
}
