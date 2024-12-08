// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;
using Keda.Scaler.DurableTask.AzureStorage.Metadata;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHubs;

/// <summary>
/// Represents the configurations for a Durable Task Hub.
/// </summary>
public class TaskHubOptions
{
    /// <inheritdoc cref="ScalerOptions.MaxActivitiesPerWorker"/>
    [Range(1, int.MaxValue)]
    public int MaxActivitiesPerWorker { get; set; }

    /// <inheritdoc cref="ScalerOptions.MaxOrchestrationsPerWorker"/>
    [Range(1, int.MaxValue)]
    public int MaxOrchestrationsPerWorker { get; set; }

    /// <inheritdoc cref="ScalerOptions.TaskHubName"/>
    [Required]
    public string TaskHubName { get; set; } = default!;

    /// <inheritdoc cref="ScalerOptions.UseTablePartitionManagement"/>
    public bool UseTablePartitionManagement { get; set; }
}
