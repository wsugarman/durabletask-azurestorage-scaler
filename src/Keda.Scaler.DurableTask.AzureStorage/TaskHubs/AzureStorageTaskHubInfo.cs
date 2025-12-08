// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.ComponentModel.DataAnnotations;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHubs;

internal sealed class AzureStorageTaskHubInfo
{
    public required DateTimeOffset CreatedAt { get; init; }

    [Range(1, 15)]
    public required int PartitionCount { get; init; }

    public required string TaskHubName { get; init; }
}
