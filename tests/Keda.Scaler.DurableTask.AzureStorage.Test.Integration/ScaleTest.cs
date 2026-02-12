// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    public async Task GivenMultipleActivities_WhenRunningOrchestrationInstance_ThenScaleUp()
    {
        const int ExpectedActivityWorkers = 3;
        int activityCount = ExpectedActivityWorkers * _options.MaxActivitiesPerWorker;

        await WaitForScaleDownAsync(_testContext.CancellationToken);

        // Start 1 orchestration with the configured number of activities
        OrchestrationRuntimeStatus? finalStatus;
        string instanceId = await StartOrchestrationAsync(activityCount, _testContext.CancellationToken);
        try
        {
            // Assert scale (needs at least 3 workers for the activities)
            await WaitForScaleUpAsync(ExpectedActivityWorkers, t => EnsureRunningAsync(instanceId, t), _testContext.CancellationToken);

            // Assert completion
            finalStatus = await WaitForOrchestration(instanceId, _testContext.CancellationToken);
        }
        catch (Exception)
        {
            _ = await TryTerminateAsync(instanceId);
            throw;
        }

        // Ensure it completed successfully
        Assert.AreEqual(OrchestrationRuntimeStatus.Completed, finalStatus);
    }

    [TestMethod]
    [Timeout(10 * 60 * 1000, CooperativeCancellation = true)]
    public async Task GivenMultipleOrchestrationInstances_WhenRunningOrchestrationsConcurrently_ThenScaleUp()
    {
        const int OrchestrationCount = 3;

        await WaitForScaleDownAsync(_testContext.CancellationToken);

        // Start 3 orchestrations with the configured number of activities
        OrchestrationRuntimeStatus?[] finalStatuses;
        string[] instanceIds = await Task.WhenAll(Enumerable
            .Repeat(_options, OrchestrationCount)
            .Select(o => StartOrchestrationAsync(o.MaxActivitiesPerWorker, _testContext.CancellationToken)));

        try
        {
            // Assert scale (needs at least 3 workers as each orchestration will spawn the max number of activities per worker)
            await WaitForScaleUpAsync(
                OrchestrationCount,
                t => Task.WhenAll(instanceIds.Select(id => EnsureRunningAsync(id, t))),
                _testContext.CancellationToken);

            // Assert completion
            finalStatuses = await Task.WhenAll(instanceIds.Select(id => WaitForOrchestration(id, _testContext.CancellationToken)));
        }
        catch (Exception e) when (e is not AssertFailedException)
        {
            _ = await Task.WhenAll(instanceIds.Select(TryTerminateAsync));
            throw;
        }

        // Ensure it completed successfully
        foreach (OrchestrationRuntimeStatus? actual in finalStatuses)
            Assert.AreEqual(OrchestrationRuntimeStatus.Completed, actual);
    }

    private async Task<string> StartOrchestrationAsync(int activities, CancellationToken cancellationToken)
    {
        string instanceId = await _durableClient.ScheduleNewOrchestrationInstanceAsync(
            new TaskName("RunAsync"),
            new { ActivityCount = activities, ActivityTime = _options.ActivityDuration },
            cancellationToken).ConfigureAwait(false);

        LogOrchestrationStarted(_logger, "RunAsync", instanceId);
        return instanceId;
    }

    private async Task EnsureRunningAsync(string instanceId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        OrchestrationMetadata? metadata = await _durableClient
            .GetInstanceAsync(instanceId, cancellationToken)
            .ConfigureAwait(false);

        Assert.IsNotNull(metadata);
        Assert.IsTrue(
            metadata.RuntimeStatus is OrchestrationRuntimeStatus.Pending or OrchestrationRuntimeStatus.Running,
            $"Instance '{instanceId}' has status '{metadata?.RuntimeStatus}'.");
    }

    private async Task<OrchestrationRuntimeStatus?> WaitForOrchestration(string instanceId, CancellationToken cancellationToken)
    {
        OrchestrationMetadata? metadata;

        LogOrchestrationWait(_logger, instanceId);

        while (true)
        {
            await Task.Delay(_options.PollingInterval, cancellationToken).ConfigureAwait(false);

            metadata = await _durableClient
                .GetInstanceAsync(instanceId, cancellationToken)
                .ConfigureAwait(false);

            if (metadata is null || metadata.IsCompleted)
                break;

            LogOrchestrationStatus(_logger, instanceId, metadata.RuntimeStatus);
        }

        LogOrchestrationCompleted(_logger, instanceId, metadata?.RuntimeStatus);
        return metadata?.RuntimeStatus;
    }

    private async Task WaitForScaleDownAsync(CancellationToken cancellationToken)
    {
        V1Scale scale;

        LogWorkerScaleDown(_logger, _options.MinReplicas);

        do
        {
            // Wait a moment before checking the first time
            await Task.Delay(_options.PollingInterval, cancellationToken).ConfigureAwait(false);

            scale = await _kubernetes.AppsV1
                .ReadNamespacedDeploymentScaleAsync(_deployment.Name, _deployment.Namespace, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            LogKubernetesDeploymentScale(
                _logger,
                _deployment.Name!,
                _deployment.Namespace!,
                scale.Status.Replicas,
                scale.Spec.Replicas);
        } while (scale.Status.Replicas != _options.MinReplicas || scale.Spec.Replicas.GetValueOrDefault() != _options.MinReplicas);
    }

    private async Task WaitForScaleUpAsync(int min, Func<CancellationToken, Task> onPollAsync, CancellationToken cancellationToken)
    {
        V1Scale scale;

        LogWorkerScaleUp(_logger, min);

        int pollCount = 0;
        do
        {
            // Wait a moment before checking the first time
            await Task.Delay(_options.PollingInterval, cancellationToken).ConfigureAwait(false);

            // Invoke some delegate with each iteration
            await onPollAsync(cancellationToken).ConfigureAwait(false);

            scale = await _kubernetes.AppsV1
                .ReadNamespacedDeploymentScaleAsync(_deployment.Name, _deployment.Namespace, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            pollCount = ++pollCount % _options.PollingIntervalsPerLog;
            if (pollCount == 0)
            {
                LogKubernetesDeploymentScale(
                    _logger,
                    _deployment.Name!,
                    _deployment.Namespace!,
                    scale.Status.Replicas,
                    scale.Spec.Replicas);
            }
        } while (scale.Status.Replicas < min || scale.Spec.Replicas.GetValueOrDefault() < min);
    }

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
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Started '{Orchestration}' instance '{InstanceId}'.")]
    public static partial void LogOrchestrationStarted(ILogger logger, string orchestration, string instanceId);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Waiting for instance '{InstanceId}' to complete.")]
    public static partial void LogOrchestrationWait(ILogger logger, string instanceId);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Current status of instance '{InstanceId}' is '{Status}'.")]
    public static partial void LogOrchestrationStatus(ILogger logger, string instanceId, OrchestrationRuntimeStatus status);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "Instance '{InstanceId}' reached terminal status '{Status}'.")]
    public static partial void LogOrchestrationCompleted(ILogger logger, string instanceId, OrchestrationRuntimeStatus? status);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "Waiting for scale down to {Target} replicas.")]
    public static partial void LogWorkerScaleDown(ILogger logger, int target);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Information,
        Message = "Waiting for scale up to at least {Target} replicas.")]
    public static partial void LogWorkerScaleUp(ILogger logger, int target);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Information,
        Message = "Current scale for deployment '{Deployment}' in namespace '{Namespace}' is {Status}/{Spec}...")]
    public static partial void LogKubernetesDeploymentScale(ILogger logger, string deployment, string @namespace, int status, int? spec);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Information,
        Message = "Terminated instance '{InstanceId}.'")]
    public static partial void LogOrchestrationTerminated(ILogger logger, string instanceId);

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Warning,
        Message = "Error encountered when terminating instance '{InstanceId}.'")]
    public static partial void LogOrchestrationTerminationFailed(ILogger logger, Exception exception, string instanceId);
}
