// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Keda.Scaler.DurableTask.AzureStorage.Monitor;

internal readonly struct TaskHubInfo : IEquatable<TaskHubInfo>
{
    public string TaskHubName { get; init; }

    public DateTime CreatedAt { get; init; }

    public int PartitionCount { get; init; }

    [ExcludeFromCodeCoverage]
    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is TaskHubInfo other && Equals(other);

    [ExcludeFromCodeCoverage]
    public bool Equals(TaskHubInfo other)
        => TaskHubName == other.TaskHubName
        && CreatedAt == other.CreatedAt
        && PartitionCount == other.PartitionCount;

    [ExcludeFromCodeCoverage]
    public override int GetHashCode()
        => HashCode.Combine(TaskHubName, CreatedAt, PartitionCount);

    [ExcludeFromCodeCoverage]
    public static bool operator ==(TaskHubInfo left, TaskHubInfo right)
        => left.Equals(right);

    [ExcludeFromCodeCoverage]
    public static bool operator !=(TaskHubInfo left, TaskHubInfo right)
        => !left.Equals(right);
}
