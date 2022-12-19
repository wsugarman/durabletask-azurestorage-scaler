// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHub;

/// <summary>
/// Represents an algorthim determines the appropriate number of Durable Task worker instances
/// based on the current orchestrations.
/// </summary>
public interface IOrchestrationAllocator
{
    /// <summary>
    /// Gets the number of workers necessary to process the given work items per partition.
    /// </summary>
    /// <param name="partitionWorkItems">The number of active orchestration work items per partition.</param>
    /// <param name="maxOrchestrationWorkItems">
    /// The maximum number of orchestration work items that a single worker may process at any time.
    /// </param>
    /// <returns>The appropriate number of worker instances.</returns>
    int GetWorkerCount(IReadOnlyList<long> partitionWorkItems, int maxOrchestrationWorkItems);
}
