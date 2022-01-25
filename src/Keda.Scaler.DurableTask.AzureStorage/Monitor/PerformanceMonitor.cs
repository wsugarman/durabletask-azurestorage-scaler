// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Keda.Scaler.DurableTask.AzureStorage.Monitor;

/// <summary>
/// An implementation of <see cref="IPerformanceMonitor"/>
/// </summary>
internal class PerformanceMonitor : IPerformanceMonitor
{
    private readonly PerformanceMonitorSettings _settings;
    private readonly ILogger _logger;
    private readonly QueueServiceClient _client;

    public PerformanceMonitor(QueueServiceClient client, PerformanceMonitorSettings settings, ILogger logger)
    {
        _settings = EnsureArg.IsNotNull(settings, nameof(settings));
        _client = EnsureArg.IsNotNull(client, nameof(client));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1849:Call async methods when in an async method", Justification = "It's OK to get task result after task completes.")]
    public async Task<PerformanceHeartbeat?> GetHeartbeatAsync()
    {
        PerformanceHeartbeat heartbeat = new PerformanceHeartbeat();
        QueueClient workItemQueue = GetWorkItemQueue();
        QueueClient[] controlQueues = GetControlQueues();
        var tasks = new List<Task>(controlQueues.Length + 1);
        Task<CloudQueueMetric> workItemMetricTask = GetQueueMetricsAsync(workItemQueue);
        List<Task<CloudQueueMetric>> controlQueueMetricTasks = controlQueues.Select(GetQueueMetricsAsync).ToList();

        tasks.Add(workItemMetricTask);
        tasks.AddRange(controlQueueMetricTasks);
        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (StorageException e) when (e.RequestInformation?.HttpStatusCode == 404)
        {

            // The queues are not yet provisioned.
            _logger.LogWarning(
                "Task hub {TaskHubName} in {AccountName} has not been provisioned: {ErrorMessage}",
                _settings.TaskHubName,
                _client.AccountName,
                e.RequestInformation.ExtendedErrorInformation?.ErrorMessage);

            return null;
        }

        heartbeat.WorkItemQueueMetric = workItemMetricTask.Result;
        heartbeat.ControlQueueMetrics = controlQueueMetricTasks.Select(item => item.Result).ToList();
        heartbeat.PartitionCount = controlQueues.Length;
        return heartbeat;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1849:Call async methods when in an async method", Justification = "It's OK to get result after task is completed.")]
    private async Task<CloudQueueMetric> GetQueueMetricsAsync(QueueClient queue)
    {
        Task<TimeSpan> latencyTask = GetQueueLatencyAsync(queue);
        Task<int> lengthTask = GetQueueLengthAsync(queue);
        await Task.WhenAll(latencyTask, lengthTask).ConfigureAwait(false);

        TimeSpan latency = latencyTask.Result;
        int length = lengthTask.Result;

        if (latency == TimeSpan.MinValue)
        {
            // No available queue messages (peek returned null)
            latency = TimeSpan.Zero;
            length = 0;
        }

        return new CloudQueueMetric { Latency = latency, Length = length };
    }

    private static async Task<TimeSpan> GetQueueLatencyAsync(QueueClient queue)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        PeekedMessage firstMessage = await queue.PeekMessageAsync().ConfigureAwait(false);
        if (firstMessage == null)
        {
            return TimeSpan.MinValue;
        }

        // Make sure we always return a non-negative timespan in the success case.
        TimeSpan latency = now.Subtract(firstMessage.InsertedOn.GetValueOrDefault(now));
        return latency < TimeSpan.Zero ? TimeSpan.Zero : latency;
    }

    static async Task<int> GetQueueLengthAsync(QueueClient queue)
    {
        var properties = await queue.GetPropertiesAsync().ConfigureAwait(false);
        return properties.Value.ApproximateMessagesCount;
    }

    private QueueClient GetWorkItemQueue()
    {
        string queueName = GetWorkItemQueueName();
        return _client.GetQueueClient(queueName);
    }

    private QueueClient[] GetControlQueues()
    {
        QueueClient[] result = new QueueClient[_settings.PartitionCount];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = _client.GetQueueClient(GetControlQueueName(i));
        }
        return result;
    }

    private string GetControlQueueName(int partitionIndex)
    {
        return GetQueueName(_settings.TaskHubName, $"control-{partitionIndex:00}");
    }

    private string GetWorkItemQueueName()
    {
        return GetQueueName(_settings.TaskHubName, "workitems");
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "durable task framework use lowercase queue name.")]
    private static string GetQueueName(string taskHub, string suffix)
    {
        EnsureArg.IsNotEmptyOrWhiteSpace(taskHub, nameof(taskHub));
        return $"{taskHub.ToLowerInvariant()}-{suffix}";
    }
}
