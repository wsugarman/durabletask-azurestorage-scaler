// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHub;

/// <summary>
/// Represents a manager for orchestration partitions in a Durable Task Hub that uses the Azure Storage backend.
/// </summary>
public interface ITaskHubPartitionManager
{
    /// <summary>
    /// Asynchronously enumerates the partition IDs used by the Durable Task Hub.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A asynchronous enumerable of zero or more partition IDs.
    /// </returns>
    IAsyncEnumerable<string> GetPartitionsAsync(CancellationToken cancellationToken = default);
}
