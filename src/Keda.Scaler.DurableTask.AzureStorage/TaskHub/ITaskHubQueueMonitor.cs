// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHub;

/// <summary>
/// Represents a monitor for a given Durable Task Hub's Azure Queues when using the Azure Storage backend provider.
/// </summary>
public interface ITaskHubQueueMonitor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TaskHubQueueMonitor"/> class.
    /// </summary>
    /// <param name="taskHubInfo">Metadata concerning the Task Hub.</param>
    /// <param name="queueServiceClient">A client for accessing the Azure Queue service.</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="taskHubInfo"/>, <paramref name="queueServiceClient"/>,
    /// or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <see cref="AzureStorageTaskHubInfo.PartitionCount"/> is less than <c>1</c> for <paramref name="taskHubInfo"/>.
    /// </exception>
    ValueTask<TaskHubQueueUsage> GetUsageAsync(CancellationToken cancellationToken = default);
}
