// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHubs;

/// <summary>
/// Represents a Durable Task Hub using the Azure Storage backend provider.
/// </summary>
public interface ITaskHub
{
    /// <summary>
    /// Asynchronously fetches the number of messages in queue across the Task Hub to gauge its usage.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A value task that represents the asynchronous operation. The value of the type parameter
    /// of the value task contains the usage for the Task Hub.
    /// </returns>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> is canceled.</exception>
    ValueTask<TaskHubQueueUsage> GetUsageAsync(CancellationToken cancellationToken = default);
}
