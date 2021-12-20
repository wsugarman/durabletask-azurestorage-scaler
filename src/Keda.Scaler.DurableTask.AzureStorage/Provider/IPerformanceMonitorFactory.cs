// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

namespace Keda.Scaler.DurableTask.AzureStorage.Provider
{
    internal interface IPerformanceMonitorFactory
    {
        ValueTask<IPerformanceMonitor> CreateAsync(ScalerMetadata metadata, CancellationToken cancellationToken = default);
    }
}
