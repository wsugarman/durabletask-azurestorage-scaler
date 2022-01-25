// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;

namespace Keda.Scaler.DurableTask.AzureStorage.Monitor;

internal readonly struct TaskHubInfo : IEquatable<TaskHubInfo>
{
    public string TaskHubName { get; init; }

    public DateTime CreatedAt { get; init; }

    public int PartitionCount { get; init; }

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is TaskHubInfo other && Equals(other);

    public bool Equals(TaskHubInfo other)
        => TaskHubName == other.TaskHubName
        && CreatedAt == other.CreatedAt
        && PartitionCount == other.PartitionCount;

    public override int GetHashCode()
        => HashCode.Combine(TaskHubName, CreatedAt, PartitionCount);

    public static bool operator ==(TaskHubInfo left, TaskHubInfo right)
        => left.Equals(right);

    public static bool operator !=(TaskHubInfo left, TaskHubInfo right)
        => !left.Equals(right);
}
