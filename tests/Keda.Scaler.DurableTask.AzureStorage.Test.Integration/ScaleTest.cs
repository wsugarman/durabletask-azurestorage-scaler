// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using DurableTask.AzureStorage;
using DurableTask.Core;
using k8s;
using k8s.Models;
using Keda.Scaler.DurableTask.AzureStorage.Test.Integration.DependencyInjection;
using Keda.Scaler.DurableTask.AzureStorage.Test.Integration.K8s;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Integration;

[TestClass]
public sealed partial class ScaleTest : IAsyncDisposable
{
    private readonly TestContext _testContext;
    private readonly ILogger _logger;
    private readonly IKubernetes _kubernetes;
    private readonly DurableTaskClient _durableClient;
    private readonly AzureStorageDurableTaskClientOptions _taskHub;
    private readonly FunctionDeploymentOptions _deployment;
    private readonly ScaleTestOptions _options;
    private readonly ServiceProvider _serviceProvider;

    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .AddEnvironmentVariables()
        .Build();

    public ScaleTest(TestContext testContext)
    {
        ArgumentNullException.ThrowIfNull(testContext, nameof(testContext));

        _testContext = testContext;

        IServiceCollection services = new ServiceCollection()
            .AddSingleton(Configuration);

        _ = services
            .AddOptions<AzureStorageDurableTaskClientOptions>()
            .Bind(Configuration.GetSection(AzureStorageDurableTaskClientOptions.DefaultSectionName))
            .ValidateDataAnnotations();

        _ = services
            .AddOptions<KubernetesOptions>()
            .Bind(Configuration.GetSection(KubernetesOptions.DefaultSectionName))
            .ValidateDataAnnotations();

        _ = services
            .AddOptions<FunctionDeploymentOptions>()
            .Bind(Configuration.GetSection(FunctionDeploymentOptions.DefaultSectionName))
            .ValidateDataAnnotations();

        _ = services
            .AddOptions<ScaleTestOptions>()
            .Bind(Configuration.GetSection(ScaleTestOptions.DefaultSectionName))
            .ValidateDataAnnotations();

        _serviceProvider = services
            .AddSingleton(_testContext)
            .AddLogging(b => b.AddUnitTesting())
            .AddSingleton(sp => sp
                .GetRequiredService<IOptions<AzureStorageDurableTaskClientOptions>>()
                .Value
                .ToOrchestrationServiceSettings())
            .AddSingleton<IOrchestrationServiceClient>(sp => new AzureStorageOrchestrationService(sp.GetRequiredService<AzureStorageOrchestrationServiceSettings>()))
            .AddSingleton(sp => sp
                .GetRequiredService<IOptions<KubernetesOptions>>()
                .Value
                .ToClientConfiguration())
            .AddSingleton<IKubernetes>(sp => new Kubernetes(sp.GetRequiredService<KubernetesClientConfiguration>()))
            .AddDurableTaskClient(b => b.UseOrchestrationService())
            .BuildServiceProvider();

        _logger = _serviceProvider.GetRequiredService<ILogger<ScaleTest>>();
        _kubernetes = _serviceProvider.GetRequiredService<IKubernetes>();
        _durableClient = _serviceProvider.GetRequiredService<DurableTaskClient>();
        _taskHub = _serviceProvider.GetRequiredService<IOptions<AzureStorageDurableTaskClientOptions>>().Value;
        _deployment = _serviceProvider.GetRequiredService<IOptions<FunctionDeploymentOptions>>().Value;
        _options = _serviceProvider.GetRequiredService<IOptions<ScaleTestOptions>>().Value;
    }

    public async ValueTask DisposeAsync()
    {
        _kubernetes.Dispose();
        await _serviceProvider.DisposeAsync();
        await _durableClient.DisposeAsync();
    }

    [TestMethod]
    [Timeout(10 * 60 * 1000, CooperativeCancellation = true)]
    public async Task GivenMultipleActivities_WhenRunningOrchestrationInstance_ThenScaleUpAndDown()
    {
        const int ExpectedActivityWorkers = 3;
        int activityCount = ExpectedActivityWorkers * _options.MaxActivitiesPerWorker;

        await WaitForScaleDownAsync(_testContext.CancellationToken);

        // Start 1 orchestration with enough activities to require at least 3 workers
        string instanceId = await StartTestOrchestrationAsync(activityCount, _testContext.CancellationToken);

        await AssertScaleAsync(
            [instanceId],
            ExpectedActivityWorkers,
            GetMaxReplicas(activityCount, orchestrationCount: 1),
            _testContext.CancellationToken);
    }

    [TestMethod]
    [Timeout(10 * 60 * 1000, CooperativeCancellation = true)]
    public async Task GivenMultipleOrchestrationInstances_WhenRunningOrchestrationsConcurrently_ThenScaleUpAndDown()
    {
        const int OrchestrationCount = 3;
        int activityCount = OrchestrationCount * _options.MaxActivitiesPerWorker;

        await WaitForScaleDownAsync(_testContext.CancellationToken);

        // Start 3 orchestrations that each spawn the max number of activities per worker
        string[] instanceIds = await Task.WhenAll(Enumerable
            .Repeat(_options.MaxActivitiesPerWorker, OrchestrationCount)
            .Select(activities => StartTestOrchestrationAsync(activities, _testContext.CancellationToken)));

        await AssertScaleAsync(
            instanceIds,
            OrchestrationCount,
            GetMaxReplicas(activityCount, OrchestrationCount),
            _testContext.CancellationToken);
    }

    [TestMethod]
    [Timeout(10 * 60 * 1000, CooperativeCancellation = true)]
    public async Task GivenPendingTimerInEachPartition_WhenRunningOrchestrations_ThenScaleByPartition()
    {
        int partitionCount = _taskHub.PartitionCount;

        // Every partition has exactly 1 pending message, so each worker can cover up to the max number of orchestrations
        // per worker. A scaler that ignores the limit and requests 1 worker per partition would not settle on this value
        int expectedReplicas = Math.Max(
            _options.MinReplicas,
            (partitionCount + _options.MaxOrchestrationsPerWorker - 1) / _options.MaxOrchestrationsPerWorker);

        await WaitForScaleDownAsync(_testContext.CancellationToken);

        // Start 1 orchestration per partition. Each pending timer is a message in its partition's control queue,
        // so every partition remains active until the timers fire
        string[] instanceIds = await Task.WhenAll(Enumerable
            .Range(0, partitionCount)
            .Select(p => StartTimerAsync(CreateInstanceId(p), _testContext.CancellationToken)));

        await EnsureActiveControlQueuesAsync(_testContext.CancellationToken);

        // While an orchestration starts, its partition may briefly contain both the ExecutionStarted message and the
        // pending timer such that the scaler temporarily requests more workers. However, the deployment must settle on
        // the expected number of replicas while the timers are pending, and it never needs more than 1 worker per partition
        await AssertScaleAsync(
            instanceIds,
            minTargetReplicas: expectedReplicas,
            maxTargetReplicas: expectedReplicas,
            maxReplicas: GetMaxReplicas(activityCount: 0, partitionCount),
            _testContext.CancellationToken);
    }

    private Task AssertScaleAsync(string[] instanceIds, int minReplicas, int maxReplicas, CancellationToken cancellationToken)
        => AssertScaleAsync(instanceIds, minReplicas, int.MaxValue, maxReplicas, cancellationToken);

    // Waits for the deployment to reach the target scale while the orchestrations are still in progress. Afterwards,
    // asserts that the orchestrations complete successfully, that the deployment never exceeds the max replicas, and
    // that the deployment scales back down
    private async Task AssertScaleAsync(string[] instanceIds, int minTargetReplicas, int maxTargetReplicas, int maxReplicas, CancellationToken cancellationToken)
    {
        int observedMaxReplicas;
        OrchestrationRuntimeStatus?[] finalStatuses;

        try
        {
            // Assert scale while the orchestrations are still in progress
            int scaleUpMaxReplicas = await WaitForScaleAsync(
                instanceIds,
                minTargetReplicas,
                maxTargetReplicas,
                cancellationToken);

            // Continue monitoring the scale until the orchestrations complete
            (finalStatuses, int completionMaxReplicas) = await WaitForOrchestrationsAsync(instanceIds, cancellationToken);
            observedMaxReplicas = Math.Max(scaleUpMaxReplicas, completionMaxReplicas);
        }
        catch (Exception)
        {
            _ = await Task.WhenAll(instanceIds.Select(TryTerminateAsync));
            throw;
        }

        // Ensure the orchestrations completed successfully
        foreach (OrchestrationRuntimeStatus? actual in finalStatuses)
            Assert.AreEqual(OrchestrationRuntimeStatus.Completed, actual);

        // Ensure the scaler never requested more workers than the work required
        Assert.IsLessThanOrEqualTo(
            maxReplicas,
            observedMaxReplicas,
            $"Deployment scaled to {observedMaxReplicas} replicas, but the work required at most {maxReplicas}.");

        // Ensure the scaler reports the idle Task Hub as inactive such that KEDA scales the deployment back down
        await AssertScaleDownAsync(cancellationToken);
    }

    private async Task AssertScaleDownAsync(CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(_options.ScaleDownTimeout);

        try
        {
            await WaitForScaleDownAsync(timeoutSource.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            Assert.Fail($"Deployment did not scale down to {_options.MinReplicas} replicas within {_options.ScaleDownTimeout}.");
        }
    }

    // The scaler requests enough workers to process the pending activities,
    // plus at most 1 worker for each partition with pending orchestration messages
    private int GetMaxReplicas(int activityCount, int orchestrationCount)
    {
        int activityWorkers = (activityCount + _options.MaxActivitiesPerWorker - 1) / _options.MaxActivitiesPerWorker;
        int orchestrationWorkers = Math.Min(orchestrationCount, _taskHub.PartitionCount);
        return Math.Max(_options.MinReplicas, activityWorkers + orchestrationWorkers);
    }

    private async Task<string> StartTestOrchestrationAsync(int activityCount, CancellationToken cancellationToken)
    {
        const string Orchestration = "RunAsync";

        string instanceId = await _durableClient.ScheduleNewOrchestrationInstanceAsync(
            new TaskName(Orchestration),
            new { ActivityCount = activityCount, ActivityTime = _options.ActivityDuration },
            cancellationToken).ConfigureAwait(false);

        LogOrchestrationStarted(_logger, Orchestration, instanceId);
        return instanceId;
    }

    private async Task<string> StartTimerAsync(string instanceId, CancellationToken cancellationToken)
    {
        const string Orchestration = "WaitForTimerAsync";

        string scheduledInstanceId = await _durableClient.ScheduleNewOrchestrationInstanceAsync(
            new TaskName(Orchestration),
            _options.TimerDuration,
            new StartOrchestrationOptions(instanceId),
            cancellationToken).ConfigureAwait(false);

        LogOrchestrationStarted(_logger, Orchestration, scheduledInstanceId);
        return scheduledInstanceId;
    }

    // The Durable Task Azure Storage provider assigns each instance to the partition (control queue) given by
    // the 32-bit FNV-1a hash of its UTF-8 encoded ID modulo the partition count
    private string CreateInstanceId(int partition)
    {
        string instanceId;
        do
        {
            instanceId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        } while (GetPartition(instanceId) != partition);

        return instanceId;
    }

    private int GetPartition(string instanceId)
    {
        const uint FnvPrime = 16777619;
        const uint FnvOffsetBasis = 2166136261;

        uint hash = FnvOffsetBasis;
        foreach (byte b in Encoding.UTF8.GetBytes(instanceId))
            hash = unchecked((hash ^ b) * FnvPrime);

        return (int)(hash % (uint)_taskHub.PartitionCount);
    }

    private async Task EnsureActiveControlQueuesAsync(CancellationToken cancellationToken)
    {
        QueueServiceClient queueServiceClient = new(_taskHub.ConnectionString);
        for (int partition = 0; partition < _taskHub.PartitionCount; partition++)
        {
            QueueClient controlQueue = queueServiceClient.GetQueueClient(GetControlQueueName(partition));
            QueueProperties properties = await controlQueue.GetPropertiesAsync(cancellationToken).ConfigureAwait(false);

            Assert.IsGreaterThan(0, properties.ApproximateMessagesCount, $"Control queue '{controlQueue.Name}' has no pending messages.");
        }
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Queue names must be lowercase.")]
    private string GetControlQueueName(int partition)
        => string.Format(CultureInfo.InvariantCulture, "{0}-control-{1:D2}", _taskHub.TaskHubName.ToLowerInvariant(), partition);

    private async Task EnsureRunningAsync(string instanceId, V1Scale? scale, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        OrchestrationMetadata? metadata = await _durableClient
            .GetInstanceAsync(instanceId, cancellationToken)
            .ConfigureAwait(false);

        Assert.IsNotNull(metadata);
        Assert.IsTrue(
            metadata.RuntimeStatus is OrchestrationRuntimeStatus.Pending or OrchestrationRuntimeStatus.Running,
            $"Instance '{instanceId}' has status '{metadata?.RuntimeStatus}' before the deployment reached its target scale. Last observed scale: {scale?.Status.Replicas}/{scale?.Spec.Replicas}.");
    }

    private async Task<(OrchestrationRuntimeStatus?[] Statuses, int MaxReplicas)> WaitForOrchestrationsAsync(string[] instanceIds, CancellationToken cancellationToken)
    {
        int maxReplicas = 0;
        int pollCount = 0;
        OrchestrationMetadata?[] instances;

        foreach (string instanceId in instanceIds)
            LogOrchestrationWait(_logger, instanceId);

        while (true)
        {
            await Task.Delay(_options.PollingInterval, cancellationToken).ConfigureAwait(false);

            V1Scale scale = await GetScaleAsync(cancellationToken).ConfigureAwait(false);
            maxReplicas = Math.Max(maxReplicas, scale.Spec.Replicas.GetValueOrDefault());

            instances = await Task
                .WhenAll(instanceIds.Select(id => _durableClient.GetInstanceAsync(id, cancellationToken)))
                .ConfigureAwait(false);

            if (instances.All(x => x is null || x.IsCompleted))
                break;

            pollCount = ++pollCount % _options.PollingIntervalsPerLog;
            if (pollCount == 0)
            {
                LogKubernetesDeploymentScale(_logger, _deployment.Name!, _deployment.Namespace!, scale.Status.Replicas, scale.Spec.Replicas);
                foreach (OrchestrationMetadata metadata in instances.OfType<OrchestrationMetadata>().Where(x => !x.IsCompleted))
                    LogOrchestrationStatus(_logger, metadata.InstanceId, metadata.RuntimeStatus);
            }
        }

        for (int i = 0; i < instanceIds.Length; i++)
            LogOrchestrationCompleted(_logger, instanceIds[i], instances[i]?.RuntimeStatus);

        return ([.. instances.Select(x => x?.RuntimeStatus)], maxReplicas);
    }

    private async Task WaitForScaleDownAsync(CancellationToken cancellationToken)
    {
        V1Scale scale;

        LogWorkerScaleDown(_logger, _options.MinReplicas);

        do
        {
            // Wait a moment before checking the first time
            await Task.Delay(_options.PollingInterval, cancellationToken).ConfigureAwait(false);

            scale = await GetScaleAsync(cancellationToken).ConfigureAwait(false);
            LogKubernetesDeploymentScale(_logger, _deployment.Name!, _deployment.Namespace!, scale.Status.Replicas, scale.Spec.Replicas);
        } while (scale.Status.Replicas != _options.MinReplicas || scale.Spec.Replicas.GetValueOrDefault() != _options.MinReplicas);
    }

    private async Task<int> WaitForScaleAsync(string[] instanceIds, int min, int max, CancellationToken cancellationToken)
    {
        V1Scale? scale = null;
        int maxReplicas = 0;

        if (min == max)
            LogWorkerScaleExact(_logger, min);
        else
            LogWorkerScaleUp(_logger, min);

        int pollCount = 0;
        do
        {
            // Wait a moment before checking the first time
            await Task.Delay(_options.PollingInterval, cancellationToken).ConfigureAwait(false);

            // The target scale must be reached while the orchestrations are still in progress
            V1Scale? lastScale = scale;
            await Task
                .WhenAll(instanceIds.Select(id => EnsureRunningAsync(id, lastScale, cancellationToken)))
                .ConfigureAwait(false);

            scale = await GetScaleAsync(cancellationToken).ConfigureAwait(false);
            maxReplicas = Math.Max(maxReplicas, scale.Spec.Replicas.GetValueOrDefault());

            pollCount = ++pollCount % _options.PollingIntervalsPerLog;
            if (pollCount == 0)
                LogKubernetesDeploymentScale(_logger, _deployment.Name!, _deployment.Namespace!, scale.Status.Replicas, scale.Spec.Replicas);
        } while (!IsWithinRange(scale.Status.Replicas) || !IsWithinRange(scale.Spec.Replicas.GetValueOrDefault()));

        return maxReplicas;

        bool IsWithinRange(int replicas) => replicas >= min && replicas <= max;
    }

    private Task<V1Scale> GetScaleAsync(CancellationToken cancellationToken)
        => _kubernetes.AppsV1.ReadNamespacedDeploymentScaleAsync(_deployment.Name, _deployment.Namespace, cancellationToken: cancellationToken);

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Ignore errors as this is a test clean up.")]
    private async Task<bool> TryTerminateAsync(string instanceId)
    {
        try
        {
            await _durableClient.TerminateInstanceAsync(instanceId, CancellationToken.None).ConfigureAwait(false);
            LogOrchestrationTerminated(_logger, instanceId);
            return true;
        }
        catch (Exception e)
        {
            LogOrchestrationTerminationFailed(_logger, e, instanceId);
            return false;
        }
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Started '{Orchestration}' instance '{InstanceId}'.")]
    public static partial void LogOrchestrationStarted(ILogger logger, string orchestration, string instanceId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Waiting for instance '{InstanceId}' to complete.")]
    public static partial void LogOrchestrationWait(ILogger logger, string instanceId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Current status of instance '{InstanceId}' is '{Status}'.")]
    public static partial void LogOrchestrationStatus(ILogger logger, string instanceId, OrchestrationRuntimeStatus status);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Instance '{InstanceId}' reached terminal status '{Status}'.")]
    public static partial void LogOrchestrationCompleted(ILogger logger, string instanceId, OrchestrationRuntimeStatus? status);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Waiting for scale down to {Target} replicas.")]
    public static partial void LogWorkerScaleDown(ILogger logger, int target);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Waiting for scale up to at least {Target} replicas.")]
    public static partial void LogWorkerScaleUp(ILogger logger, int target);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Waiting for scale to exactly {Target} replicas.")]
    public static partial void LogWorkerScaleExact(ILogger logger, int target);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Current scale for deployment '{Deployment}' in namespace '{Namespace}' is {Status}/{Spec}...")]
    public static partial void LogKubernetesDeploymentScale(ILogger logger, string deployment, string @namespace, int status, int? spec);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Terminated instance '{InstanceId}.'")]
    public static partial void LogOrchestrationTerminated(ILogger logger, string instanceId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Error encountered when terminating instance '{InstanceId}.'")]
    public static partial void LogOrchestrationTerminationFailed(ILogger logger, Exception exception, string instanceId);
}
