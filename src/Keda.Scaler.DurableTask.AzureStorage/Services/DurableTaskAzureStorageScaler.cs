// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Keda.Scaler.DurableTask.AzureStorage.Provider;
using Microsoft.Extensions.Logging;

namespace Keda.Scaler.DurableTask.AzureStorage.Services
{
    internal class DurableTaskAzureStorageScaler : IDurableTaskAzureStorageScaler
    {
        private readonly AzureClientFactory _azureClientFactory;
        private readonly ITaskHubBrowser _taskHubBrowser;
        private readonly IPerformanceMonitor _performanceMonitor;
        private readonly ILogger<DurableTaskAzureStorageScaler> _logger;

        public DurableTaskAzureStorageScaler(
            AzureClientFactory azureClientFactory,
            ITaskHubBrowser taskHubBrowser,
            IPerformanceMonitor performanceMonitor,
            ILogger<DurableTaskAzureStorageScaler> logger)
        {
            _azureClientFactory = azureClientFactory ?? throw new ArgumentNullException(nameof(azureClientFactory));
            _taskHubBrowser = taskHubBrowser ?? throw new ArgumentNullException(nameof(taskHubBrowser));
            _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask<ScalerMetrics> GetMetricsAsync(ScalerMetadata metadata, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(metadata.TaskHubName))
                throw new ArgumentException($"{nameof(ScalerMetadata.TaskHubName)} must be specified.", nameof(metadata));

            TaskHubInfo taskHubInfo = await _taskHubBrowser.GetAsync(GetBlobServiceClient(metadata), metadata.TaskHubName, cancellationToken).ConfigureAwait(false);
            if (taskHubInfo == default)
                throw new InvalidOperationException("Cannot resolve task hub");

            QueueServiceClient queueServiceClient = GetQueueServiceClient(metadata);
            ValueTask<QueueMetrics> workItemMetricsTask = _performanceMonitor.GetWorkItemMetricsAsync(queueServiceClient, taskHubInfo, cancellationToken);
            IAsyncEnumerable<QueueMetrics> controlMetrics = _performanceMonitor.GetControlMetricsAsync(queueServiceClient, taskHubInfo, cancellationToken);

            (int Count, int MaxLength, long MaxLatencyMs) controlQueues = await AggregateMetricsAsync(controlMetrics).ConfigureAwait(false);
            return new ScalerMetrics
            {
                ControlQueueDemand = taskHubInfo.PartitionCount * taskHubInfo.PartitionCount,
                ControlQueueLatencyMs = metadata.TargetMessageLatencyMs,
                WorkItemQueueLatencyMs = metadata.TargetMessageLatencyMs,
            };
        }

        public async ValueTask<ScalerMetrics> GetMetricSpecAsync(ScalerMetadata metadata, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(metadata.TaskHubName))
                throw new ArgumentException($"{nameof(ScalerMetadata.TaskHubName)} must be specified.", nameof(metadata));

            TaskHubInfo taskHubInfo = await _taskHubBrowser.GetAsync(GetBlobServiceClient(metadata), metadata.TaskHubName, cancellationToken).ConfigureAwait(false);
            if (taskHubInfo == default)
                throw new InvalidOperationException("Cannot resolve task hub");

            return new ScalerMetrics
            {
                ControlQueueDemand = taskHubInfo.PartitionCount,
                ControlQueueLatencyMs = metadata.TargetMessageLatencyMs,
                WorkItemQueueLatencyMs = metadata.TargetMessageLatencyMs,
            };
        }

        public ValueTask<bool> IsActiveAsync(ScalerMetadata metadata, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        private BlobServiceClient GetBlobServiceClient(ScalerMetadata metadata)
            => metadata.AccountName is null
            ? _azureClientFactory.GetBlobServiceClient(metadata.ConnectionString!)
            : _azureClientFactory.GetBlobServiceClient(metadata.AccountName, CloudEndpoints.ForEnvironment(metadata.Cloud));

        private QueueServiceClient GetQueueServiceClient(ScalerMetadata metadata)
            => metadata.AccountName is null
            ? _azureClientFactory.GetQueueServiceClient(metadata.ConnectionString!)
            : _azureClientFactory.GetQueueServiceClient(metadata.AccountName, CloudEndpoints.ForEnvironment(metadata.Cloud));

        private static async ValueTask<(int Count, int MaxLength, long MaxLatencyMs)> AggregateMetricsAsync(IAsyncEnumerable<QueueMetrics> metrics)
        {
            (int Count, int MaxLength, long MaxLatencyMs) aggregate = (0, 0, 0);
            await foreach (QueueMetrics metric in metrics)
            {
                aggregate.Count++;
                aggregate.MaxLength = Math.Max(aggregate.MaxLength, metric.Length);
                aggregate.MaxLatencyMs = Math.Max(aggregate.MaxLatencyMs, (long)metric.DequeueLatency.TotalMilliseconds);
            }

            return aggregate;
        }
    }
}
