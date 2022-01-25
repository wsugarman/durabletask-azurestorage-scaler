// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Keda.Scaler.DurableTask.AzureStorage.Monitor;

class PerformanceMonitor : IPerformanceMonitor
{
    private readonly PerformanceMonitorSettings _settings;
    private readonly ILogger _logger;
    private readonly CloudQueueClient _client;

    public PerformanceMonitor(CloudQueueClient client, PerformanceMonitorSettings settings, ILogger logger)
    {
        _settings = EnsureArg.IsNotNull(settings, nameof(settings));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        _client = EnsureArg.IsNotNull(client, nameof(client));
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1849:Call async methods when in an async method", Justification = "It's OK to get task result after task completes.")]
    public async Task<PerformanceHeartbeat?> GetHeartbeatAsync()
    {
        PerformanceHeartbeat result = new PerformanceHeartbeat();

        CloudQueue workItemQueue = GetWorkItemQueue();
        CloudQueue[] controlQueues = GetControlQueues();
        Task<QueueMetric> workItemMetricTask = GetQueueMetricsAsync(workItemQueue);
        List<Task<QueueMetric>> controlQueueMetricTasks = controlQueues.Select(GetQueueMetricsAsync).ToList();
        var tasks = new List<Task>(controlQueueMetricTasks.Count + 1);
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
                "Task hub {TaskHubName} in {QueueBaseUri} has not been provisioned: {ErrorMessage}",
                _settings.TaskHubName,
                _client.BaseUri,
                e.RequestInformation.ExtendedErrorInformation?.ErrorMessage);

            return null;
        }

        result.WorkItemQueueMetric = workItemMetricTask.Result;
        result.ControlQueueMetrixs = controlQueueMetricTasks.Select(item => item.Result).ToList();
        result.PartitionCount = controlQueues.Length;
        return result;
    }

    private async Task<QueueMetric> GetQueueMetricsAsync(CloudQueue queue)
    {
        Task<TimeSpan> latencyTask = GetQueueLatencyAsync(queue);
        Task<int> lengthTask = GetQueueLengthAsync(queue);
        await Task.WhenAll(latencyTask, lengthTask).ConfigureAwait(false);

        TimeSpan latency = await latencyTask.ConfigureAwait(false);
        int length = await lengthTask.ConfigureAwait(false);

        if (latency == TimeSpan.MinValue)
        {
            // No available queue messages (peek returned null)
            latency = TimeSpan.Zero;
            length = 0;
        }

        return new QueueMetric { Latency = latency, Length = length };
    }

    private static async Task<TimeSpan> GetQueueLatencyAsync(CloudQueue queue)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        CloudQueueMessage firstMessage = await queue.PeekMessageAsync().ConfigureAwait(false);
        if (firstMessage == null)
        {
            return TimeSpan.MinValue;
        }

        // Make sure we always return a non-negative timespan in the success case.
        TimeSpan latency = now.Subtract(firstMessage.InsertionTime.GetValueOrDefault(now));
        return latency < TimeSpan.Zero ? TimeSpan.Zero : latency;
    }

    static async Task<int> GetQueueLengthAsync(CloudQueue queue)
    {
        // Update attributes
        await queue.FetchAttributesAsync().ConfigureAwait(false);
        return queue.ApproximateMessageCount.GetValueOrDefault(0);
    }

    private CloudQueue GetWorkItemQueue()
    {
        string queueName = GetWorkItemQueueName();
        return _client.GetQueueReference(queueName);
    }

    private CloudQueue[] GetControlQueues()
    {
        CloudQueue[] result = new CloudQueue[_settings.PartitionCount];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = _client.GetQueueReference(GetControlQueueName(i));
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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "<Pending>")]
    private static string GetQueueName(string taskHub, string suffix)
    {
        EnsureArg.IsNotEmptyOrWhiteSpace(taskHub, nameof(taskHub));
        return $"{taskHub.ToLowerInvariant()}-{suffix}";
    }
}
