// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

namespace Keda.Scaler.DurableTask.AzureStorage.Services
{
    public interface IDurableTaskAzureStorageScaler
    {
        ValueTask<bool> IsActiveAsync(ScalerMetadata metadata, CancellationToken cancellationToken = default);

        ValueTask<ScalerMetrics> GetMetricSpecAsync(ScalerMetadata metadata, CancellationToken cancellationToken = default);

        ValueTask<ScalerMetrics> GetMetricsAsync(ScalerMetadata metadata, CancellationToken cancellationToken = default);
    }
}
