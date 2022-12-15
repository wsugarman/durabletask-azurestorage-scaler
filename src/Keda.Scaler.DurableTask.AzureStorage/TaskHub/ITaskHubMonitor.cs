// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHub;

/// <summary>
/// Represents a client that may be used to monitor the usage of a Durable Task Hub.
/// </summary>
public interface ITaskHubMonitor
{
    /// <summary>
    /// Asynchronously retrieves the usage for the Task Hub.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A value task that represents the asynchronous operation. The value of the type parameter
    /// of the value task contains Task Hub usage.
    /// </returns>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> is canceled.</exception>
    ValueTask<TaskHubUsage> GetUsageAsync(CancellationToken cancellationToken = default);
}
