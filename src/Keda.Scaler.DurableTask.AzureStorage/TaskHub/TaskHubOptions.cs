// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHub;

/// <summary>
/// Represents the configurations for a Durable Task Hub.
/// </summary>
public class TaskHubOptions
{
    /// <inheritdoc cref="ScalerMetadata.MaxActivitiesPerWorker"/>
    [Range(1, int.MaxValue)]
    public int MaxActivitiesPerWorker { get; set; }

    /// <inheritdoc cref="ScalerMetadata.MaxOrchestrationsPerWorker"/>
    [Range(1, int.MaxValue)]
    public int MaxOrchestrationsPerWorker { get; set; }

    /// <inheritdoc cref="ScalerMetadata.TaskHubName"/>
    [Required]
    public string TaskHubName { get; set; } = default!;

    /// <inheritdoc cref="ScalerMetadata.UseTablePartitionManagement"/>
    public bool UseTablePartitionManagement { get; set; }
}
