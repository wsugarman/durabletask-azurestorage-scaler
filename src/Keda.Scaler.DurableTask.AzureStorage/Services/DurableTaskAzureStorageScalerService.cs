// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Grpc.Core;

namespace Keda.Scaler.DurableTask.AzureStorage.Services
{
    public class DurableTaskAzureStorageScalerService : ExternalScaler.ExternalScalerBase
    {
        public override Task<IsActiveResponse> IsActive(ScaledObjectRef request, ServerCallContext context)
        {
            return Task.FromResult(new IsActiveResponse());
        }

        public override Task StreamIsActive(ScaledObjectRef request, IServerStreamWriter<IsActiveResponse> responseStream, ServerCallContext context)
        {
            return Task.CompletedTask;
        }

        public override Task<GetMetricSpecResponse> GetMetricSpec(ScaledObjectRef request, ServerCallContext context)
        {
            return Task.FromResult(new GetMetricSpecResponse());
        }

        public override Task<GetMetricsResponse> GetMetrics(GetMetricsRequest request, ServerCallContext context)
        {
            return Task.FromResult(new GetMetricsResponse());
        }
    }
}
