// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.AzureStorage.Monitoring;
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

    public async ValueTask<long> GetMetricValueAsync(KubernetesResource scaledObject, ScalerMetadata metadata, CancellationToken cancellationToken = default)
    {
        if (metadata is null)
            throw new ArgumentNullException(nameof(metadata));

        int replicaCount = await GetCurrentScaleAsync(scaledObject, cancellationToken).ConfigureAwait(false);

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

        double scaleFactor = (heartbeat?.ScaleRecommendation.Action ?? ScaleAction.None) switch
        {
            ScaleAction.None => 1d,
            ScaleAction.AddWorker => metadata.ScaleIncrement,
            ScaleAction.RemoveWorker => 1 / metadata.ScaleIncrement,
            _ => throw new InvalidOperationException($"Unknown scale action '{heartbeat!.ScaleRecommendation.Action}'."),
        };

        // Note: Currently only average metric value are supported by external scalers,
        //       so we need to multiply the result by the number of replicas.
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

    private async ValueTask<int> GetCurrentScaleAsync(KubernetesResource scaledObjRef, CancellationToken cancellationToken)
    {
        V1ScaledObject scaledObj = await _kubernetes.ReadNamespacedScaledObjectAsync(scaledObjRef.Name, scaledObjRef.Namespace, cancellationToken).ConfigureAwait(false);

        V1KedaScaleTarget scaleTarget = scaledObj.Spec.ScaleTargetRef;
        (string group, string version) = ApiVersion.Split(scaleTarget.ApiVersion ?? "apps/v1");
        string kind = scaleTarget.Kind ?? "Deployment";

        V1Scale scale = await _kubernetes.ReadNamespacedCustomObjectScaleAsync(
            scaleTarget.Name,
            scaledObjRef.Namespace,
            group,
            version,
            kind,
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Found current scale for '{Kind}.{Version}/{Name}' in namespace '{Namespace}' to be {Replicas} replicas.",
            kind,
            version,
            scaledObjRef.Name,
            scaledObjRef.Namespace,
            scale.Status.Replicas);

        return scale.Status.Replicas;
    }
}
