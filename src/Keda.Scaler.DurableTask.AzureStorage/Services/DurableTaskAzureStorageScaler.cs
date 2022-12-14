// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using Keda.Scaler.DurableTask.AzureStorage.Extensions;
using Keda.Scaler.DurableTask.AzureStorage.Kubernetes;
using Keda.Scaler.DurableTask.AzureStorage.Provider;
using Microsoft.Extensions.Logging;

namespace Keda.Scaler.DurableTask.AzureStorage.Services;

internal sealed class DurableTaskAzureStorageScaler : IDurableTaskAzureStorageScaler
{
    internal const long MetricSpecValue = 100;

    private readonly IKubernetes _kubernetes;
    private readonly IPerformanceMonitorFactory _monitorFactory;
    private readonly ILogger _logger;

    public string MetricName => "WorkerDemand";

    public DurableTaskAzureStorageScaler(
        IKubernetes kubernetes,
        IPerformanceMonitorFactory monitorFactory,
        ILoggerFactory loggerFactory)
    {
        _kubernetes = kubernetes ?? throw new ArgumentNullException(nameof(kubernetes));
        _monitorFactory = monitorFactory ?? throw new ArgumentNullException(nameof(monitorFactory));
        _logger = loggerFactory?.CreateLogger(Diagnostics.LoggerCategory) ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public ValueTask<long> GetMetricSpecAsync(ScalerMetadata metadata, CancellationToken cancellationToken = default)
    {
        if (metadata is null)
            throw new ArgumentNullException(nameof(metadata));

        return ValueTask.FromResult(MetricSpecValue);
    }

    public async ValueTask<long> GetMetricValueAsync(ScaledObjectReference scaledObjRef, ScalerMetadata metadata, CancellationToken cancellationToken = default)
    {
        if (metadata is null)
            throw new ArgumentNullException(nameof(metadata));

        int replicaCount = await GetCurrentScaleAsync(scaledObjRef, cancellationToken).ConfigureAwait(false);

        using IPerformanceMonitor monitor = await _monitorFactory.CreateAsync(metadata, cancellationToken).ConfigureAwait(false);
        PerformanceHeartbeat? heartbeat = await monitor.GetHeartbeatAsync(replicaCount).ConfigureAwait(false);
        if (heartbeat is null)
        {
            _logger.LogWarning("Failed to measure Durable Task performance");
        }
        else
        {
            _logger.LogInformation(
                "Recommendation is to {Recommendation} due to reason: '{Reason}'",
                heartbeat.ScaleRecommendation.Action,
                heartbeat.ScaleRecommendation.Reason);
        }

        // Note: Currently only average metric value are supported by external scalers,
        //       so we need to multiply the result by the number of replicas.
        //       If the replicaCount is 0 or invalid, then we need to adjust the calculation
        ScaleAction scaleAction = heartbeat?.ScaleRecommendation.Action ?? ScaleAction.None;
        if (replicaCount <= 0)
        {
            return scaleAction switch
            {
                ScaleAction.None or ScaleAction.RemoveWorker => MetricSpecValue,
                ScaleAction.AddWorker => (long)(MetricSpecValue * metadata.ScaleIncrement),
                _ => throw new InvalidOperationException(SR.Format(SR.UnknownScaleActionFormat, scaleAction)),
            };
        }

        double scaleFactor = scaleAction switch
        {
            ScaleAction.None => 1d,
            ScaleAction.AddWorker => metadata.ScaleIncrement,
            ScaleAction.RemoveWorker => 1 / metadata.ScaleIncrement,
            _ => throw new InvalidOperationException(SR.Format(SR.UnknownScaleActionFormat, scaleAction)),
        };

        return (long)(MetricSpecValue * scaleFactor * replicaCount);
    }

    public async ValueTask<bool> IsActiveAsync(ScalerMetadata metadata, CancellationToken cancellationToken = default)
    {
        if (metadata is null)
            throw new ArgumentNullException(nameof(metadata));

        using IPerformanceMonitor monitor = await _monitorFactory.CreateAsync(metadata, cancellationToken).ConfigureAwait(false);
        PerformanceHeartbeat? heartbeat = await monitor.GetHeartbeatAsync().ConfigureAwait(false);
        return heartbeat is not null && !heartbeat.IsIdle();
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Normalize Kubernetes kind to lowercase")]
    private async ValueTask<int> GetCurrentScaleAsync(ScaledObjectReference scaledObjRef, CancellationToken cancellationToken)
    {
        V1ScaledObject scaledObj = await _kubernetes.CustomObjects.ReadNamespacedScaledObjectAsync(scaledObjRef.Name, scaledObjRef.Namespace, cancellationToken).ConfigureAwait(false);

        V1ScaleTargetRef scaleTarget = scaledObj.Spec.ScaleTargetRef;
        (string group, string version) = scaleTarget.ApiVersion is not null ? scaleTarget.ApiGroupAndVersion() : ("apps", "v1");
        string kind = scaleTarget.Kind ?? "Deployment";

        V1Scale scale = await _kubernetes.CustomObjects.ReadNamespacedCustomObjectScaleAsync(
            scaleTarget.Name,
            scaledObjRef.Namespace,
            group,
            version,
            kind,
            cancellationToken).ConfigureAwait(false);

        int replicaCount = scale.Status?.Replicas ?? 0;
        _logger.LogInformation(
            "Found current scale for '{Kind}.{Group}/{Name}' in namespace '{Namespace}' to be {Replicas} replicas.",
            kind.ToLowerInvariant(),
            group,
            scaledObjRef.Name,
            scaledObjRef.Namespace,
            replicaCount);

        return replicaCount;
    }
}
