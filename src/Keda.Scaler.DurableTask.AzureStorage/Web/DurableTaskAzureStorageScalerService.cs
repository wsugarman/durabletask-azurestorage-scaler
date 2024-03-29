// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Google.Protobuf.Collections;
using Grpc.Core;
using Keda.Scaler.DurableTask.AzureStorage.Accounts;
using Keda.Scaler.DurableTask.AzureStorage.Protobuf;
using Keda.Scaler.DurableTask.AzureStorage.TaskHub;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.Web;

/// <summary>
/// Implements the KEDA external scaler gRPC service for the Durable Task framework with an Azure Storage backend provider.
/// </summary>
public class DurableTaskAzureStorageScalerService : ExternalScaler.ExternalScalerBase
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

    private readonly IServiceProvider _serviceProvider;
    private readonly AzureStorageTaskHubClient _taskHubClient;
    private readonly IOrchestrationAllocator _partitionAllocator;
    private readonly ILogger _logger;

    internal const string MetricName = "TaskHubScale";

    private static readonly ValidateScalerMetadata MetadataValidator = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DurableTaskAzureStorageScalerService"/> class
    /// with the given service container.
    /// </summary>
    /// <param name="serviceProvider">An <see cref="IServiceProvider"/> whose services are used to determine the necessary scale.</param>
    /// <exception cref="ArgumentNullException"><paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
    public DurableTaskAzureStorageScalerService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _taskHubClient = _serviceProvider.GetRequiredService<AzureStorageTaskHubClient>();
        _partitionAllocator = _serviceProvider.GetRequiredService<IOrchestrationAllocator>();
        _logger = _serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(LogCategories.Default);
    }

    /// <summary>
    /// Asynchronously returns the metric values measured by the KEDA external scaler.
    /// </summary>
    /// <param name="request">A metrics requet containing both the scalar metadata and the target Kubernetes resource.</param>
    /// <param name="context">The gRPC context.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The value of its <see cref="Task{TResult}.Result"/>
    /// property contains the 1 or more metric values.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="request"/> or <paramref name="context"/> is <see langword="null"/>.
    /// </exception>
    public override async Task<GetMetricsResponse> GetMetrics(GetMetricsRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        ScalerMetadata metadata = ParseScalerMetadata(request.ScaledObjectRef.ScalerMetadata);
        AzureStorageAccountInfo accountInfo = metadata.GetAccountInfo();

        ITaskHubQueueMonitor monitor = await _taskHubClient
            .GetMonitorAsync(accountInfo, metadata.TaskHubName, context.CancellationToken)
            .ConfigureAwait(false);

        TaskHubQueueUsage usage = await monitor.GetUsageAsync(context.CancellationToken).ConfigureAwait(false);
        int workerCount = _partitionAllocator.GetWorkerCount(usage.ControlQueueMessages, metadata.MaxOrchestrationsPerWorker);
        long metricValue = usage.WorkItemQueueMessages + (workerCount * metadata.MaxActivitiesPerWorker);

        _logger.ComputedScalerMetricValue(metadata.TaskHubName, metricValue);
        return new GetMetricsResponse
        {
            MetricValues =
            {
                new MetricValue
                {
                    MetricName = MetricName,
                    MetricValue_ = metricValue,
                },
            },
        };
    }

    /// <summary>
    /// Asynchronously returns the metric specification measured by the KEDA external scaler.
    /// </summary>
    /// <param name="request">A KEDA <see cref="ScaledObjectRef"/>.</param>
    /// <param name="context">The gRPC context.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The value of its <see cref="Task{TResult}.Result"/>
    /// property contains the 1 or more metric specifications.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="request"/> or <paramref name="context"/> is <see langword="null"/>.
    /// </exception>
    public override Task<GetMetricSpecResponse> GetMetricSpec(ScaledObjectRef request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        ScalerMetadata metadata = ParseScalerMetadata(request.ScalerMetadata);

        _logger.ComputedScalerMetricTarget(metadata.TaskHubName, metadata.MaxActivitiesPerWorker);
        return Task.FromResult(
            new GetMetricSpecResponse
            {
                MetricSpecs =
                {
                    new MetricSpec
                    {
                        MetricName = MetricName,
                        TargetSize = metadata.MaxActivitiesPerWorker,
                    },
                },
            });
    }

    /// <summary>
    /// Asynchronously indicates whether a subsequent call to <see cref="GetMetrics"/> is necessary based
    /// on the state of the Durable Task storage provider.
    /// </summary>
    /// <param name="request">A KEDA <see cref="ScaledObjectRef"/>.</param>
    /// <param name="context">The gRPC context.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The value of its <see cref="Task{TResult}.Result"/>
    /// property is <see langword="true"/> if <see cref="GetMetrics"/> should be invoked;
    /// otherwise, it's <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="request"/> or <paramref name="context"/> is <see langword="null"/>.
    /// </exception>
    public override async Task<IsActiveResponse> IsActive(ScaledObjectRef request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        ScalerMetadata metadata = ParseScalerMetadata(request.ScalerMetadata);
        AzureStorageAccountInfo accountInfo = metadata.GetAccountInfo();

        ITaskHubQueueMonitor monitor = await _taskHubClient
            .GetMonitorAsync(accountInfo, metadata.TaskHubName, context.CancellationToken)
            .ConfigureAwait(false);

        TaskHubQueueUsage usage = await monitor.GetUsageAsync(context.CancellationToken).ConfigureAwait(false);

        if (usage.HasActivity)
        {
            _logger.DetectedActiveTaskHub(metadata.TaskHubName);
            return new IsActiveResponse { Result = true };
        }
        else
        {
            _logger.DetectedInactiveTaskHub(metadata.TaskHubName);
            return new IsActiveResponse { Result = false };
        }
    }

    private static ScalerMetadata ParseScalerMetadata(MapField<string, string> mapField)
    {
        ArgumentNullException.ThrowIfNull(mapField);

        ScalerMetadata metadata = new();
        mapField.ToConfiguration().Bind(metadata);

        ValidateOptionsResult result = MetadataValidator.Validate(null, metadata);
        if (result.Failed)
            throw new ValidationException(result.FailureMessage);

        return metadata;
    }
}
