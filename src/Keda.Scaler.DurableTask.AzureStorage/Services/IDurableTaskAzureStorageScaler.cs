// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Keda.Scaler.DurableTask.AzureStorage.Kubernetes;

namespace Keda.Scaler.DurableTask.AzureStorage.Services;

/// <summary>
/// Represents a scaler for the Durable Task Framework that leverages Azure Storage for its backend provider.
/// </summary>
public interface IDurableTaskAzureStorageScaler
{
    /// <summary>
    /// Asynchronously retrieves the metric specification based on the <paramref name="metadata"/>.
    /// </summary>
    /// <param name="metadata">The external scaler's metadata.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A value task that represents the asynchronous operation. The value of the type parameter
    /// of the value task contains the target value for the metric.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="metadata"/> is <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> is canceled.</exception>
    ValueTask<long> GetMetricSpecAsync(ScalerMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves the metric value based on the <paramref name="metadata"/>.
    /// </summary>
    /// <param name="scaledObjRef">The reference to the <c>ScaledObject</c> resource.</param>
    /// <param name="metadata">The external scaler's metadata.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A value task that represents the asynchronous operation. The value of the type parameter
    /// of the value task contains the current value for the metric.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="metadata"/> is <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> is canceled.</exception>
    ValueTask<long> GetMetricValueAsync(ScaledObjectReference scaledObjRef, ScalerMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously indicates whether the scaler is active due to durable functions or actors.
    /// </summary>
    /// <param name="metadata">The external scaler's metadata.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A value task that represents the asynchronous operation. The value of the type parameter
    /// of the value task is <see langword="true"/> if active; otherwise, it's <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="metadata"/> is <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> is canceled.</exception>
    ValueTask<bool> IsActiveAsync(ScalerMetadata metadata, CancellationToken cancellationToken = default);
}
