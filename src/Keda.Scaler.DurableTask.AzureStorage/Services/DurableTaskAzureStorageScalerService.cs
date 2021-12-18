// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Grpc.Core;
using Keda.Scaler.DurableTask.AzureStorage.Extensions;
using Microsoft.Extensions.Configuration;

namespace Keda.Scaler.DurableTask.AzureStorage.Services
{
    /// <summary>
    /// Implements the KEDA external scaler gRPC service for DurableTask with an Azure Storage backend.
    /// </summary>
    public class DurableTaskAzureStorageScalerService : ExternalScaler.ExternalScalerBase
    {
        private readonly IDurableTaskAzureStorageScaler _scaler;

        /// <summary>
        /// Initializes a new instance of the <see cref="DurableTaskAzureStorageScalerService"/> class
        /// that encapsulates an instance of the <see cref="IDurableTaskAzureStorageScaler"/> class.
        /// </summary>
        /// <param name="scaler">A <see cref="IDurableTaskAzureStorageScaler"/> used for determining the proper scale.</param>
        /// <exception cref="ArgumentNullException"><paramref name="scaler"/> is <see langword="null"/>.</exception>
        public DurableTaskAzureStorageScalerService(IDurableTaskAzureStorageScaler scaler)
            => _scaler = scaler ?? throw new ArgumentNullException(nameof(scaler));

        public override async Task<IsActiveResponse> IsActive(ScaledObjectRef request, ServerCallContext context)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            if (context is null)
                throw new ArgumentNullException(nameof(context));

            ScalerMetadata metadata = request.ScalerMetadata.ToConfiguration().Get<ScalerMetadata>();
            return new IsActiveResponse { Result = await _scaler.IsActiveAsync(metadata, context.CancellationToken).ConfigureAwait(false) };
        }

        public override async Task<GetMetricSpecResponse> GetMetricSpec(ScaledObjectRef request, ServerCallContext context)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            if (context is null)
                throw new ArgumentNullException(nameof(context));

            ScalerMetadata metadata = request.ScalerMetadata.ToConfiguration().Get<ScalerMetadata>();
            metadata.EnsureValidated();

            ScalerMetrics optimalMetrics = await _scaler.GetMetricSpecAsync(metadata, context.CancellationToken).ConfigureAwait(false);

            var response = new GetMetricSpecResponse();
            response.MetricSpecs.AddRange(optimalMetrics.ToMetricSpecs());
            return response;
        }

        public override async Task<GetMetricsResponse> GetMetrics(GetMetricsRequest request, ServerCallContext context)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            if (context is null)
                throw new ArgumentNullException(nameof(context));

            ScalerMetadata metadata = request.ScaledObjectRef.ScalerMetadata.ToConfiguration().Get<ScalerMetadata>();
            metadata.EnsureValidated();

            ScalerMetrics optimalMetrics = await _scaler.GetMetricsAsync(metadata, context.CancellationToken).ConfigureAwait(false);

            var response = new GetMetricsResponse();
            response.MetricValues.AddRange(optimalMetrics.ToMetricValues());
            return response;
        }
    }
}
