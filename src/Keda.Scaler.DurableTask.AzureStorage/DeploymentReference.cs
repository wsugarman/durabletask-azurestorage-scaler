// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;

namespace Keda.Scaler.DurableTask.AzureStorage
{
    /// <summary>
    /// Represents a reference to a Kubernetes deployment.
    /// </summary>
    public readonly struct DeploymentReference : IEquatable<DeploymentReference>
    {
        /// <summary>
        /// Gets the unique name for the deployment within the <see cref="Namespace"/>.
        /// </summary>
        /// <value>The name of the deployment resource.</value>
        public string Name { get; }

        /// <summary>
        /// Gets the namespace that contains the deployment.
        /// </summary>
        /// <value>The kubernetes namespace.</value>
        public string Namespace { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeploymentReference"/> structure
        /// with the specified <paramref name="name"/> and <paramref name="namespace"/>.
        /// </summary>
        /// <param name="name">The unique name of the deployment within the <paramref name="namespace"/>.</param>
        /// <param name="namespace">The kubernetes namespace containing the deployment.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="name"/> or <paramref name="namespace"/> is <see langword="null"/>, empty, or consists
        /// entirely of white space characters.
        /// </exception>
        public DeploymentReference(string name, string @namespace = "default")
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            if (string.IsNullOrWhiteSpace(@namespace))
                throw new ArgumentNullException(nameof(@namespace));

            Name = name;
            Namespace = @namespace;
        }

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">The object to compare to this instance.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="obj"/> is an instance of <see cref="DeploymentReference"/>
        /// and equals the value of this instance; otherwise, <see langword="false"/>.
        /// </returns>
        public override bool Equals(object? obj)
            => obj is DeploymentReference other && Equals(other);

        /// <summary>
        /// Returns a value indicating whether the value of this instance is equal
        /// to the value of the specified <see cref="DeploymentReference"/> instance.
        /// </summary>
        /// <param name="other">The object to compare to this instance.</param>
        /// <returns>
        /// <see langword="true"/> if the <paramref name="other"/> parameter equals the value of
        /// this instance; otherwise, <see langword="false"/>.
        /// </returns>
        public bool Equals(DeploymentReference other)
            => Name == other.Name && Namespace == other.Namespace;

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
            => HashCode.Combine(Name, Namespace);

        /// <summary>
        /// Determines whether two specified instances of <see cref="DeploymentReference"/> are equal.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> represent
        /// the same <see cref="DeploymentReference"/>; otherwise, <see langword="true"/>.
        /// </returns>
        public static bool operator ==(DeploymentReference left, DeploymentReference right)
            => left.Equals(right);

        /// <summary>
        /// Determines whether two specified instances of <see cref="DeploymentReference"/> are not equal.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> do not represent
        /// the same <see cref="DeploymentReference"/>; otherwise, <see langword="true"/>.
        /// </returns>
        public static bool operator !=(DeploymentReference left, DeploymentReference right)
            => !left.Equals(right);
    }
}
