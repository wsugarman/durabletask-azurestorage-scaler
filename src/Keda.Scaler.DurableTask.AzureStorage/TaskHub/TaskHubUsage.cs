// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHub;

/// <summary>
/// Represents the current usage for a particular Durable Task Hub.
/// </summary>
public readonly struct TaskHubUsage : IEquatable<TaskHubUsage>
{
    /// <summary>
    /// Gets the number of ongoing activities.
    /// </summary>
    /// <value>The non-negative number of activities.</value>
    public long CurrentActivityCount { get; init; }

    /// <summary>
    /// Gets the number of ongoing orchestrations.
    /// </summary>
    /// <value>The non-negative number of orchestrations.</value>
    public long CurrentOrchestrationCount { get; init; }

    /// <summary>
    /// Returns a value indicating whether this instance is equal to a specified object.
    /// </summary>
    /// <param name="obj">The object to compare to this instance.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="obj"/> is an instance of <see cref="TaskHubUsage"/>
    /// and equals the value of this instance; otherwise, <see langword="false"/>.
    /// </returns>
    public override bool Equals(object? obj)
        => obj is TaskHubUsage other && Equals(other);

    /// <summary>
    /// Returns a value indicating whether the value of this instance is equal
    /// to the value of the specified <see cref="TaskHubUsage"/> instance.
    /// </summary>
    /// <param name="other">The object to compare to this instance.</param>
    /// <returns>
    /// <see langword="true"/> if the <paramref name="other"/> parameter equals the value of
    /// this instance; otherwise, <see langword="false"/>.
    /// </returns>
    public bool Equals(TaskHubUsage other)
        => CurrentActivityCount == other.CurrentActivityCount && CurrentOrchestrationCount == other.CurrentOrchestrationCount;

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public override int GetHashCode()
        => HashCode.Combine(CurrentActivityCount, CurrentOrchestrationCount);

    /// <summary>
    /// Determines whether two specified instances of <see cref="TaskHubUsage"/> are equal.
    /// </summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> represent
    /// the same <see cref="TaskHubUsage"/>; otherwise, <see langword="true"/>.
    /// </returns>
    public static bool operator ==(TaskHubUsage left, TaskHubUsage right)
        => left.Equals(right);

    /// <summary>
    /// Determines whether two specified instances of <see cref="TaskHubUsage"/> are not equal.
    /// </summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> do not represent
    /// the same <see cref="TaskHubUsage"/>; otherwise, <see langword="true"/>.
    /// </returns>
    public static bool operator !=(TaskHubUsage left, TaskHubUsage right)
        => !left.Equals(right);
}
