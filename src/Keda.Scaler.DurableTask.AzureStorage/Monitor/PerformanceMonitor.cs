// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Keda.Scaler.DurableTask.AzureStorage.Monitor;

/// <summary>
/// An implementation of <see cref="IPerformanceMonitor"/>
/// </summary>

[ExcludeFromCodeCoverage(Justification = "Cannot mock QueueClient for Moq bug https://github.com/moq/moq4/issues/991")]
internal class PerformanceMonitor : IPerformanceMonitor
{
    private readonly PerformanceMonitorOptions _options;
    private readonly ILogger<PerformanceMonitor> _logger;
    private readonly QueueServiceClient _queueServiceClient;
    private readonly ITaskHubBrowser _taskHubBrowser;

    public PerformanceMonitor(
        QueueServiceClient queueServiceclient,
        ITaskHubBrowser taskHubBrowser,
        IOptionsSnapshot<PerformanceMonitorOptions> options,
        ILogger<PerformanceMonitor> logger)
    {
        _options = EnsureArg.IsNotNull(options?.Value, nameof(options));
        _queueServiceClient = EnsureArg.IsNotNull(queueServiceclient, nameof(queueServiceclient));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        _taskHubBrowser = EnsureArg.IsNotNull(taskHubBrowser, nameof(taskHubBrowser));
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1849:Call async methods when in an async method", Justification = "It's OK to get task result after task completes.")]
    public async Task<PerformanceHeartbeat?> GetHeartbeatAsync(CancellationToken cancellationToken)
    {
        TaskHubInfo? taskHubInfo = await _taskHubBrowser.GetAsync(_options.TaskHubName, cancellationToken).ConfigureAwait(false);

        if (taskHubInfo == null)
        {
            return null;
        }

        PerformanceHeartbeat heartbeat = new PerformanceHeartbeat();
        QueueClient workItemQueue = GetWorkItemQueue();
        QueueClient[] controlQueues = GetControlQueues();
        var tasks = new List<Task>(controlQueues.Length + 1);
        Task<QueueMetric> workItemMetricTask = GetQueueMetricsAsync(workItemQueue, cancellationToken);
        List<Task<QueueMetric>> controlQueueMetricTasks = controlQueues.Select(x => GetQueueMetricsAsync(x, cancellationToken)).ToList();

        tasks.Add(workItemMetricTask);
        tasks.AddRange(controlQueueMetricTasks);
        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (StorageException e) when (e.RequestInformation?.HttpStatusCode == (int)HttpStatusCode.NotFound)
        {
            // The queues are not yet provisioned.
            _logger.LogWarning(
                "Task hub {TaskHubName} in {AccountName} has not been provisioned: {ErrorMessage}",
                _options.TaskHubName,
                _queueServiceClient.AccountName,
                e.RequestInformation.ExtendedErrorInformation?.ErrorMessage);

            return null;
        }

        heartbeat.WorkItemQueueMetric = workItemMetricTask.Result;
        heartbeat.ControlQueueMetrics = controlQueueMetricTasks.Select(item => item.Result).ToList();
        heartbeat.PartitionCount = controlQueues.Length;
        return heartbeat;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1849:Call async methods when in an async method", Justification = "It's OK to get result after task is completed.")]
    private static async Task<QueueMetric> GetQueueMetricsAsync(QueueClient queue, CancellationToken cancellationToken)
    {
        Task<TimeSpan> latencyTask = GetQueueLatencyAsync(queue, cancellationToken);
        Task<int> lengthTask = GetQueueLengthAsync(queue, cancellationToken);
        await Task.WhenAll(latencyTask, lengthTask).ConfigureAwait(false);

        TimeSpan latency = latencyTask.Result;
        int length = lengthTask.Result;

        if (latency == TimeSpan.MinValue)
        {
            // No available queue messages (peek returned null)
            latency = TimeSpan.Zero;
            length = 0;
        }

        return new QueueMetric { Latency = latency, Length = length };
    }

    private static async Task<TimeSpan> GetQueueLatencyAsync(QueueClient queue, CancellationToken cancellationToken)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        PeekedMessage firstMessage = await queue.PeekMessageAsync(cancellationToken).ConfigureAwait(false);
        if (firstMessage == null)
        {
            return TimeSpan.MinValue;
        }

        // Make sure we always return a non-negative timespan in the success case.
        TimeSpan latency = now.Subtract(firstMessage.InsertedOn.GetValueOrDefault(now));
        return latency < TimeSpan.Zero ? TimeSpan.Zero : latency;
    }

    private static async Task<int> GetQueueLengthAsync(QueueClient queue, CancellationToken cancellationToken)
    {
        var properties = await queue.GetPropertiesAsync(cancellationToken).ConfigureAwait(false);
        return properties.Value.ApproximateMessagesCount;
    }

    private QueueClient GetWorkItemQueue()
    {
        string queueName = GetWorkItemQueueName();
        return _queueServiceClient.GetQueueClient(queueName);
    }

    private QueueClient[] GetControlQueues()
    {
        QueueClient[] result = new QueueClient[_options.PartitionCount];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = _queueServiceClient.GetQueueClient(GetControlQueueName(i));
        }
        return result;
    }

    private string GetControlQueueName(int partitionIndex)
    {
        return GetQueueName(_options.TaskHubName, $"control-{partitionIndex:00}");
    }

    private string GetWorkItemQueueName()
    {
        return GetQueueName(_options.TaskHubName, "workitems");
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "durable task framework use lowercase queue name.")]
    private static string GetQueueName(string taskHub, string suffix)
    {
        EnsureArg.IsNotEmptyOrWhiteSpace(taskHub, nameof(taskHub));
        return $"{taskHub.ToLowerInvariant()}-{suffix}";
    }
}
