// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHubs;

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
    /// A value task that represents the asynchronous operation. The value of the type parameter
    /// of the value task contains a list of the partition IDs.
    /// </returns>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> is canceled.</exception>
    ValueTask<IReadOnlyList<string>> GetPartitionsAsync(CancellationToken cancellationToken = default);
}
