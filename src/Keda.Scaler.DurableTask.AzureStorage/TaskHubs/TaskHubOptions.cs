// Copyright © William Sugarman.
// Licensed under the MIT License.

using Keda.Scaler.DurableTask.AzureStorage.Metadata;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHubs;

/// <summary>
/// Represents the configurations for a Durable Task Hub.
/// </summary>
public class TaskHubOptions
{
    /// <inheritdoc cref="ScalerOptions.MaxActivitiesPerWorker"/>
    public int MaxActivitiesPerWorker { get; set; }

    /// <inheritdoc cref="ScalerOptions.MaxOrchestrationsPerWorker"/>
    public int MaxOrchestrationsPerWorker { get; set; }

    /// <inheritdoc cref="ScalerOptions.TaskHubName"/>
    public string TaskHubName { get; set; } = default!;

    /// <inheritdoc cref="ScalerOptions.UseTablePartitionManagement"/>
    public bool UseTablePartitionManagement { get; set; }
}
