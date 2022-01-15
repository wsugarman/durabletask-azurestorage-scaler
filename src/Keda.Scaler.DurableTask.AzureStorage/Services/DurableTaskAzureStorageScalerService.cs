// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Grpc.Core;
using Keda.Scaler.DurableTask.AzureStorage.Extensions;
using Keda.Scaler.DurableTask.AzureStorage.Kubernetes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Keda.Scaler.DurableTask.AzureStorage.Services;

/// <summary>
/// Implements the KEDA external scaler gRPC service for the Durable Task framework with an Azure Storage backend provider.
/// </summary>
public class DurableTaskAzureStorageScalerService : ExternalScaler.ExternalScalerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDurableTaskAzureStorageScaler _scaler;

    /// <summary>
    /// Initializes a new instance of the <see cref="DurableTaskAzureStorageScalerService"/> class
    /// with the given service container.
    /// </summary>
    /// <param name="serviceProvider">An <see cref="IServiceProvider"/> whose services are used to determine the necessary scale.</param>
    /// <exception cref="ArgumentNullException"><paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
    public DurableTaskAzureStorageScalerService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _scaler = serviceProvider.GetRequiredService<IDurableTaskAzureStorageScaler>();
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
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (context is null)
            throw new ArgumentNullException(nameof(context));

        ScalerMetadata metadata = request.ScalerMetadata.ToConfiguration().Get<ScalerMetadata>().EnsureValidated(_serviceProvider);
        return new IsActiveResponse { Result = await _scaler.IsActiveAsync(metadata, context.CancellationToken).ConfigureAwait(false) };
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
    public override async Task<GetMetricSpecResponse> GetMetricSpec(ScaledObjectRef request, ServerCallContext context)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (context is null)
            throw new ArgumentNullException(nameof(context));

        ScalerMetadata metadata = request.ScalerMetadata.ToConfiguration().Get<ScalerMetadata>().EnsureValidated(_serviceProvider);

        GetMetricSpecResponse response = new GetMetricSpecResponse();
        response.MetricSpecs.Add(
            new MetricSpec
            {
                MetricName = _scaler.MetricName,
                TargetSize = await _scaler.GetMetricSpecAsync(metadata, context.CancellationToken).ConfigureAwait(false),
            });
        return response;
    }

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
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (context is null)
            throw new ArgumentNullException(nameof(context));

        ScalerMetadata metadata = request.ScaledObjectRef.ScalerMetadata.ToConfiguration().Get<ScalerMetadata>().EnsureValidated(_serviceProvider);
        KubernetesResource deployment = new KubernetesResource(request.ScaledObjectRef.Name, request.ScaledObjectRef.Namespace);

        GetMetricsResponse response = new GetMetricsResponse();
        response.MetricValues.Add(
            new MetricValue
            {
                MetricName = request.MetricName,
                MetricValue_ = await _scaler.GetMetricValueAsync(deployment, metadata, context.CancellationToken).ConfigureAwait(false),
            });

        return response;
    }
}
