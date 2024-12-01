// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Grpc.Core;
using Keda.Scaler.DurableTask.AzureStorage.TaskHub;

namespace Keda.Scaler.DurableTask.AzureStorage.Web;

/// <summary>
/// Implements the KEDA external scaler gRPC service for the Durable Task framework with an Azure Storage backend provider.
/// </summary>
public class DurableTaskAzureStorageScalerService : ExternalScaler.ExternalScalerBase
{
    private readonly DurableTaskScaleManager _scaleManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="DurableTaskAzureStorageScalerService"/> class
    /// with the given service container.
    /// </summary>
    /// <param name="scaleManager">The scale manager for determining how to scale the workers.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="scaleManager"/> is <see langword="null"/>.
    /// </exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0290:Use primary constructor", Justification = "Preserve separate XML doc for class and ctor.")]
    public DurableTaskAzureStorageScalerService(DurableTaskScaleManager scaleManager)
        => _scaleManager = scaleManager ?? throw new ArgumentNullException(nameof(scaleManager));

    /// <summary>
    /// Asynchronously returns the metric values measured by the KEDA external scaler.
    /// </summary>
    /// <param name="request">A metrics requet containing both the scalar metadata and the target Kubernetes resource.</param>
    /// <param name="context">The gRPC context.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The value of its <see cref="Task{TResult}.Result"/>
    /// property contains the 1 or more metric values.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="request"/> or <paramref name="context"/> is <see langword="null"/>.
    /// </exception>
    public override async Task<GetMetricsResponse> GetMetrics(GetMetricsRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        return new GetMetricsResponse
        {
            MetricValues =
            {
                await _scaleManager.GetKedaMetricValueAsync(context.CancellationToken)
            }
        };
    }

    /// <summary>
    /// Asynchronously returns the metric specification measured by the KEDA external scaler.
    /// </summary>
    /// <param name="request">A KEDA <see cref="ScaledObjectRef"/>.</param>
    /// <param name="context">The gRPC context.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The value of its <see cref="Task{TResult}.Result"/>
    /// property contains the 1 or more metric specifications.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="request"/> or <paramref name="context"/> is <see langword="null"/>.
    /// </exception>
    public override Task<GetMetricSpecResponse> GetMetricSpec(ScaledObjectRef request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        return Task.FromResult(new GetMetricSpecResponse
        {
            MetricSpecs =
            {
                _scaleManager.KedaMetricSpec,
            }
        });
    }

    /// <summary>
    /// Asynchronously indicates whether a subsequent call to <see cref="GetMetrics"/> is necessary based
    /// on the state of the Durable Task storage provider.
    /// </summary>
    /// <param name="request">A KEDA <see cref="ScaledObjectRef"/>.</param>
    /// <param name="context">The gRPC context.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The value of its <see cref="Task{TResult}.Result"/>
    /// property is <see langword="true"/> if <see cref="GetMetrics"/> should be invoked;
    /// otherwise, it's <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="request"/> or <paramref name="context"/> is <see langword="null"/>.
    /// </exception>
    public override async Task<IsActiveResponse> IsActive(ScaledObjectRef request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        return new IsActiveResponse
        {
            Result = await _scaleManager.IsActiveAsync(context.CancellationToken),
        };
    }
}
