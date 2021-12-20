// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;

namespace Keda.Scaler.DurableTask.AzureStorage.Common
{
    /// <summary>
    /// Represents the current environment encapsulating this process. This class cannot be inherited.
    /// </summary>
    public sealed class CurrentEnvironment : IProcessEnvironment
    {
        /// <summary>
        /// Gets the <see cref="IProcessEnvironment"/> instance for the current environment.
        /// </summary>
        /// <value>The current environment.</value>
        public static IProcessEnvironment Instance { get; } = new CurrentEnvironment();

        private CurrentEnvironment()
        { }

        /// <inheritdoc/>
        public string? GetEnvironmentVariable(string variable)
            => Environment.GetEnvironmentVariable(variable);
    }
}
