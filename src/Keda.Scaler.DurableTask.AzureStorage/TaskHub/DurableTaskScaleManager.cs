// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHub;

/// <summary>
/// Represents an algorthim determines the appropriate number of Durable Task worker instances
/// based on the current orchestrations.
/// </summary>
public abstract class DurableTaskScaleManager
{
    // Let R = the recommended number of workers
    //     A = the number of activity work items
    //     O = the number of orchestration work items
    //     M = the user-specified maximum activity work item count per worker
    //     N = the number of orchestration partitions per worker
    //     P = P/1 = O/N = the optimal number of workers for the orchestrations
    //
    // Then R = A/M + O/N
    //        = A/M + P/1
    //        = (A*1 + P*M)/(M*1)
    //        = (A + P*M)/M
    //
    // From the HPA definition, the computation of targetAverageValue uses the following formulua:
    // (See https://kubernetes.io/docs/tasks/run-application/horizontal-pod-autoscale/#algorithm-details)
    // Let T = the target metric
    //     V = the metric value
    //     D = the desired number of workers
    //
    // Then D = ceil[V/T] = R
    // Therefore V/T = (A + P*M)/M
    // And in turn V = A + P*M
    //             T = M

    internal const string MetricName = "TaskHubScale";

    private readonly ITaskHub _taskHub;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DurableTaskScaleManager"/> class.
    /// </summary>
    /// <param name="taskHub">The Durable Task Hub for the request.</param>
    /// <param name="options">The settings for the given Task Hub.</param>
    /// <param name="loggerFactory">A diagnostic logger factory.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="taskHub"/>, <paramref name="options"/>, or <paramref name="loggerFactory"/> is <see langword="null"/>.
    /// </exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0290:Use primary constructor", Justification = "Preserve separate XML doc for class and ctor.")]
    protected DurableTaskScaleManager(ITaskHub taskHub, IOptionsSnapshot<TaskHubOptions> options, ILoggerFactory loggerFactory)
    {
        _taskHub = taskHub ?? throw new ArgumentNullException(nameof(taskHub));
        Options = options?.Get(default) ?? throw new ArgumentNullException(nameof(options));
        _logger = loggerFactory?.CreateLogger(LogCategories.Default) ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <summary>
    /// Gets the metric specification used by KEDA for the metrics returned by <see cref="GetKedaMetricValueAsync(CancellationToken)"/>.
    /// </summary>
    /// <value>The KEDA metric specification.</value>
    public virtual MetricSpec KedaMetricSpec
    {
        get
        {
            _logger.ComputedScalerMetricTarget(Options.TaskHubName, Options.MaxActivitiesPerWorker);
            return new()
            {
                MetricName = MetricName,
                TargetSize = Options.MaxActivitiesPerWorker,
            };
        }
    }

    /// <summary>
    /// Gets the settings for the Durable Task Hub.
    /// </summary>
    /// <value>The options for the current request.</value>
    protected TaskHubOptions Options { get; }

    /// <summary>
    /// Asynchronously gets the KEDA metric value used to scale Durable Task pods.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A value task that represents the asynchronous operation. The value of the type parameter
    /// of the value task contains the metric value.
    /// </returns>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> is canceled.</exception>
    public virtual async ValueTask<MetricValue> GetKedaMetricValueAsync(CancellationToken cancellationToken = default)
    {
        TaskHubQueueUsage usage = await _taskHub.GetUsageAsync(cancellationToken);

        long metricValue = usage.HasActivity
            ? usage.WorkItemQueueMessages + (GetWorkerCount(usage) * Options.MaxActivitiesPerWorker)
            : 0;

        _logger.ComputedScalerMetricValue(Options.TaskHubName, metricValue);
        return new MetricValue
        {
            MetricName = MetricName,
            MetricValue_ = metricValue,
        };
    }

    /// <summary>
    /// Asynchronously returns a value indicating whether the Durable Task Hub is actively performing work including
    /// orchestration, activities, and entities.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A value task that represents the asynchronous operation. The value of the type parameter
    /// of the value task contains <see langword="true"/> if the Task Hub is active; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> is canceled.</exception>
    public virtual async ValueTask<bool> IsActiveAsync(CancellationToken cancellationToken = default)
    {
        TaskHubQueueUsage usage = await _taskHub.GetUsageAsync(cancellationToken);
        if (usage.HasActivity)
        {
            _logger.DetectedActiveTaskHub(Options.TaskHubName);
            return true;
        }
        else
        {
            _logger.DetectedInactiveTaskHub(Options.TaskHubName);
            return false;
        }
    }

    /// <summary>
    /// Gets the number of workers necessary to process the given work items per partition.
    /// </summary>
    /// <param name="usage">The usage for the Task Hub that has some activity.</param>
    /// <returns>Tthe appropriate number of worker instances.</returns>
    protected abstract int GetWorkerCount(TaskHubQueueUsage usage);
}
