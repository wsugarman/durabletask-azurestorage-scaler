// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.AzureStorage.Monitoring;
using k8s;
using k8s.Models;
using Keda.Scaler.DurableTask.AzureStorage.Extensions;
using Keda.Scaler.DurableTask.AzureStorage.Provider;
using Microsoft.Extensions.Logging;

namespace Keda.Scaler.DurableTask.AzureStorage.Services;

internal sealed class DurableTaskAzureStorageScaler : IDurableTaskAzureStorageScaler
{
    internal const long MetricSpecValue = 100;

    private const string LoggerCategory = "DurableTask.AzureStorage.Keda"; // Reuse prefix from the Durable Task framework

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
        _logger = loggerFactory?.CreateLogger(LoggerCategory) ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public ValueTask<long> GetMetricSpecAsync(ScalerMetadata metadata, CancellationToken cancellationToken = default)
    {
        if (metadata is null)
            throw new ArgumentNullException(nameof(metadata));

        return ValueTask.FromResult(MetricSpecValue);
    }

    public async ValueTask<long> GetMetricValueAsync(DeploymentReference deployment, ScalerMetadata metadata, CancellationToken cancellationToken = default)
    {
        if (metadata is null)
            throw new ArgumentNullException(nameof(metadata));

        V1Scale scale = await _kubernetes.ReadNamespacedDeploymentScaleAsync(deployment.Name, deployment.Namespace, cancellationToken: cancellationToken).ConfigureAwait(false);
        _logger.LogInformation(
            "Found current scale for deployment '{Name}' in namespace '{Namespace}' to be {Replicas} replicas.",
            deployment.Name,
            deployment.Namespace,
            scale.Status.Replicas);

        using IPerformanceMonitor monitor = await _monitorFactory.CreateAsync(metadata, cancellationToken).ConfigureAwait(false);
        PerformanceHeartbeat? heartbeat = await monitor.GetHeartbeatAsync(scale.Status.Replicas).ConfigureAwait(false);
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
        return (long)(MetricSpecValue * scaleFactor * scale.Status.Replicas);
    }

    public async ValueTask<bool> IsActiveAsync(ScalerMetadata metadata, CancellationToken cancellationToken = default)
    {
        if (metadata is null)
            throw new ArgumentNullException(nameof(metadata));

        using IPerformanceMonitor monitor = await _monitorFactory.CreateAsync(metadata, cancellationToken).ConfigureAwait(false);
        PerformanceHeartbeat? heartbeat = await monitor.GetHeartbeatAsync().ConfigureAwait(false);
        return heartbeat is not null && !heartbeat.IsIdle();
    }
}
