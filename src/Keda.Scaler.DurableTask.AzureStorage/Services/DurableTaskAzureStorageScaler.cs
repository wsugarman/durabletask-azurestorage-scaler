// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.AzureStorage;
using DurableTask.AzureStorage.Monitoring;
using k8s;
using k8s.Models;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Keda.Scaler.DurableTask.AzureStorage.Common;
using Keda.Scaler.DurableTask.AzureStorage.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;

namespace Keda.Scaler.DurableTask.AzureStorage.Services
{
    internal sealed class DurableTaskAzureStorageScaler : IDurableTaskAzureStorageScaler
    {
        private readonly IKubernetes _kubernetes;
        private readonly ITokenCredentialFactory _credentialFactory;
        private readonly IEnvironment _environment;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<DurableTaskAzureStorageScaler> _logger;

        private const long MetricSpecValue = 100;

        public string MetricName => "WorkerDemand";

        public DurableTaskAzureStorageScaler(
            IKubernetes kubernetes,
            ITokenCredentialFactory credentialFactory,
            IEnvironment environment,
            ILoggerFactory loggerFactory)
        {
            _kubernetes = kubernetes ?? throw new ArgumentNullException(nameof(kubernetes));
            _credentialFactory = credentialFactory ?? throw new ArgumentNullException(nameof(credentialFactory));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<DurableTaskAzureStorageScaler>();
        }

        public ValueTask<long> GetScaleMetricSpecAsync(ScalerMetadata metadata, CancellationToken cancellationToken = default)
        {
            if (metadata is null)
                throw new ArgumentNullException(nameof(metadata));

            return ValueTask.FromResult(MetricSpecValue);
        }

        public async ValueTask<long> GetScaleMetricValueAsync(DeploymentReference deployment, ScalerMetadata metadata, CancellationToken cancellationToken = default)
        {
            if (metadata is null)
                throw new ArgumentNullException(nameof(metadata));

            if (string.IsNullOrEmpty(metadata.TaskHubName))
                throw new ArgumentException($"{nameof(ScalerMetadata.TaskHubName)} must be specified.", nameof(metadata));

            V1Scale scale = await _kubernetes.ReadNamespacedDeploymentScaleAsync(deployment.Name, deployment.Namespace, cancellationToken: cancellationToken).ConfigureAwait(false);
            _logger.LogInformation(
                "Found current scale for deployment '{Name}' in namespace '{Namespace}' to be {Replicas} replicas.",
                deployment.Name,
                deployment.Namespace,
                scale.Status.Replicas);

            PerformanceHeartbeat heartbeat = await GetPerformanceHeartbeatAsync(metadata, scale.Status.Replicas, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation(
                "Recommendation is to {Recommendation} with reason: {Reason}",
                heartbeat.ScaleRecommendation.Action,
                heartbeat.ScaleRecommendation.Reason);

            double scaleFactor = heartbeat.ScaleRecommendation.Action switch
            {
                ScaleAction.None => 1d,
                ScaleAction.AddWorker => metadata.ScaleIncrement,
                ScaleAction.RemoveWorker => 1 / metadata.ScaleIncrement,
                _ => throw new InvalidOperationException($"Unknown scale action '{heartbeat.ScaleRecommendation.Action}'."),
            };

            // Note: Currently only average metric value are supported by external scalers, so we
            //       need to multiply the result by the number of replicas.
            return (long)(MetricSpecValue * scaleFactor * scale.Status.Replicas);
        }

        public async ValueTask<bool> IsActiveAsync(ScalerMetadata metadata, CancellationToken cancellationToken = default)
        {
            if (metadata is null)
                throw new ArgumentNullException(nameof(metadata));

            if (string.IsNullOrEmpty(metadata.TaskHubName))
                throw new ArgumentException($"{nameof(ScalerMetadata.TaskHubName)} must be specified.", nameof(metadata));

            PerformanceHeartbeat heartbeat = await GetPerformanceHeartbeatAsync(metadata, cancellationToken: cancellationToken).ConfigureAwait(false);
            return heartbeat is not null && !heartbeat.IsIdle();
        }

        private async ValueTask<PerformanceHeartbeat> GetPerformanceHeartbeatAsync(ScalerMetadata metadata, int? workerCount = null, CancellationToken cancellationToken = default)
        {
            AzureStorageOrchestrationServiceSettings settings = new AzureStorageOrchestrationServiceSettings
            {
                LoggerFactory = _loggerFactory,
                MaxQueuePollingInterval = TimeSpan.FromMilliseconds(metadata.MaxMessageLatencyMilliseconds),
                TaskHubName = metadata.TaskHubName,
            };

            if (metadata.AccountName is null)
            {
                DisconnectedPerformanceMonitor performanceMonitor = new DisconnectedPerformanceMonitor(
                    CloudStorageAccount.Parse(metadata.ResolveConnectionString(_environment)),
                    settings);

                return await performanceMonitor.PulseAsync(workerCount).ConfigureAwait(false);
            }
            else
            {
                CloudEndpoints endpoints = CloudEndpoints.ForEnvironment(metadata.Cloud);
                using TokenCredential tokenCredential = await _credentialFactory.CreateAsync(endpoints.AuthorityHost, cancellationToken).ConfigureAwait(false);
                DisconnectedPerformanceMonitor performanceMonitor = new DisconnectedPerformanceMonitor(
                    new CloudStorageAccount(
                        new StorageCredentials(tokenCredential),
                        metadata.AccountName,
                        endpoints.StorageSuffix,
                        useHttps: true),
                    settings);

                return await performanceMonitor.PulseAsync(workerCount).ConfigureAwait(false);
            }
        }
    }
}
