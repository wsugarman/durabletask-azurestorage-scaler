// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Grpc.Core;
using Keda.Scaler.DurableTask.AzureStorage.Accounts;
using Keda.Scaler.DurableTask.AzureStorage.Common;
using Keda.Scaler.DurableTask.AzureStorage.Extensions;
using Keda.Scaler.DurableTask.AzureStorage.TaskHub;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Keda.Scaler.DurableTask.AzureStorage;

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
    private readonly AzureStorageTaskHubBrowser _taskHubBrowser;
    private readonly IProcessEnvironment _environment;
    private readonly IOrchestrationAllocator _partitionAllocator;

    private const string MetricName = "TaskHubScale";

    /// <summary>
    /// Initializes a new instance of the <see cref="DurableTaskAzureStorageScalerService"/> class
    /// with the given service container.
    /// </summary>
    /// <param name="serviceProvider">An <see cref="IServiceProvider"/> whose services are used to determine the necessary scale.</param>
    /// <exception cref="ArgumentNullException"><paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
    public DurableTaskAzureStorageScalerService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _taskHubBrowser = _serviceProvider.GetRequiredService<AzureStorageTaskHubBrowser>();
        _environment = _serviceProvider.GetRequiredService<IProcessEnvironment>();
        _partitionAllocator = _serviceProvider.GetRequiredService<IOrchestrationAllocator>();
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
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (context is null)
            throw new ArgumentNullException(nameof(context));

        ScalerMetadata? metadata = request
            .ScalerMetadata
            .ToConfiguration()
            .Get<ScalerMetadata>()!
            .EnsureValidated(_serviceProvider);

        AzureStorageAccountInfo accountInfo = metadata.GetAccountInfo(_environment);

        ITaskHubQueueMonitor monitor = await _taskHubBrowser
            .GetMonitorAsync(accountInfo, metadata.TaskHubName, context.CancellationToken)
            .ConfigureAwait(false);

        TaskHubQueueUsage usage = await monitor.GetUsageAsync(context.CancellationToken).ConfigureAwait(false);

        return new IsActiveResponse { Result = usage.HasActivity };
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
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (context is null)
            throw new ArgumentNullException(nameof(context));

        ScalerMetadata? metadata = request
            .ScalerMetadata
            .ToConfiguration()
            .Get<ScalerMetadata>()!
            .EnsureValidated(_serviceProvider);

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
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (context is null)
            throw new ArgumentNullException(nameof(context));

        ScalerMetadata? metadata = request
            .ScaledObjectRef
            .ScalerMetadata
            .ToConfiguration()
            .Get<ScalerMetadata>()!
            .EnsureValidated(_serviceProvider);

        AzureStorageAccountInfo accountInfo = metadata.GetAccountInfo(_environment);

        ITaskHubQueueMonitor monitor = await _taskHubBrowser
            .GetMonitorAsync(accountInfo, metadata.TaskHubName, context.CancellationToken)
            .ConfigureAwait(false);

        TaskHubQueueUsage usage = await monitor.GetUsageAsync(context.CancellationToken).ConfigureAwait(false);
        return new GetMetricsResponse
        {
            MetricValues =
            {
                new MetricValue
                {
                    MetricName = MetricName,
                    MetricValue_ = usage.WorkItemQueueMessages * metadata.MaxOrchestrationsPerWorker
                        + _partitionAllocator.GetWorkerCount(usage.ControlQueueMessages, metadata.MaxOrchestrationsPerWorker),
                },
            },
        };
    }
}
