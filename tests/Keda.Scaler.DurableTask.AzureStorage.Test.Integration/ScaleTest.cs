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
public sealed class ScaleTest : IAsyncDisposable
{
    private readonly ILogger _logger;
    private readonly IKubernetes _kubernetes;
    private readonly DurableTaskClient _durableClient;
    private readonly FunctionDeploymentOptions _deployment;
    private readonly ScaleTestOptions _options;

    private static TestContext? s_testContext;
    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .AddEnvironmentVariables()
        .Build();

    [ClassInitialize]
    public static void ClassInitialize(TestContext testContext)
        => s_testContext = testContext;

    public ScaleTest()
    {
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

        IServiceProvider provider = services
            .AddLogging(b => b
                .AddSimpleConsole(
                    o =>
                    {
                        o.IncludeScopes = true;
                        o.SingleLine = false;
                        o.TimestampFormat = "O";
                        o.UseUtcTimestamp = true;
                    }))
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

        _logger = provider.GetRequiredService<ILogger<ScaleTest>>();
        _kubernetes = provider.GetRequiredService<IKubernetes>();
        _durableClient = provider.GetRequiredService<DurableTaskClient>();
        _deployment = provider.GetRequiredService<IOptions<FunctionDeploymentOptions>>().Value;
        _options = provider.GetRequiredService<IOptions<ScaleTestOptions>>().Value;
    }

    [TestMethod]
    public async Task Activities()
    {
        const int ExpectedActivityWorkers = 3;
        int activityCount = ExpectedActivityWorkers * _options.MaxActivitiesPerWorker;

        using CancellationTokenSource tokenSource = new();
        using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            s_testContext!.CancellationTokenSource.Token,
            tokenSource.Token);

        tokenSource.CancelAfter(_options.Timeout);

        await WaitForScaleDownAsync(linkedSource.Token).ConfigureAwait(false);

        // Start 1 orchestration with the configured number of activities
        OrchestrationRuntimeStatus? finalStatus;
        string instanceId = await StartOrchestrationAsync(activityCount, linkedSource.Token).ConfigureAwait(false);
        try
        {
            // Assert scale (needs at least 3 workers for the activities)
            await WaitForScaleUpAsync(ExpectedActivityWorkers, t => EnsureRunningAsync(instanceId, t), linkedSource.Token).ConfigureAwait(false);

            // Assert completion
            finalStatus = await WaitForOrchestration(instanceId, linkedSource.Token).ConfigureAwait(false);
        }
        catch (Exception)
        {
            _ = await TryTerminateAsync(instanceId, linkedSource.Token).ConfigureAwait(false);
            throw;
        }

        // Ensure it completed successfully
        Assert.AreEqual(OrchestrationRuntimeStatus.Completed, finalStatus);
    }

    [TestMethod]
    public async Task Orchestrations()
    {
        const int OrchestrationCount = 3;

        using CancellationTokenSource tokenSource = new();
        using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            s_testContext!.CancellationTokenSource.Token,
            tokenSource.Token);

        tokenSource.CancelAfter(_options.Timeout);

        await WaitForScaleDownAsync(linkedSource.Token).ConfigureAwait(false);

        // Start 3 orchestrations with the configured number of activities
        OrchestrationRuntimeStatus?[] finalStatuses;
        string[] instanceIds = await Task
            .WhenAll(Enumerable
                .Repeat(_options, OrchestrationCount)
                .Select(o => StartOrchestrationAsync(o.MaxActivitiesPerWorker, linkedSource.Token)))
            .ConfigureAwait(false);

        try
        {
            // Assert scale (needs at least 3 workers as each orchestration will spawn the max number of activities per worker)
            await WaitForScaleUpAsync(
                OrchestrationCount,
                t => Task.WhenAll(instanceIds.Select(id => EnsureRunningAsync(id, t))),
                linkedSource.Token).ConfigureAwait(false);

            // Assert completion
            finalStatuses = await Task.WhenAll(instanceIds.Select(id => WaitForOrchestration(id, linkedSource.Token))).ConfigureAwait(false);
        }
        catch (Exception e) when (e is not AssertFailedException)
        {
            _ = await Task.WhenAll(instanceIds.Select(id => TryTerminateAsync(id, linkedSource.Token))).ConfigureAwait(false);
            throw;
        }

        // Ensure it completed successfully
        foreach (OrchestrationRuntimeStatus? actual in finalStatuses)
            Assert.AreEqual(OrchestrationRuntimeStatus.Completed, actual);
    }

    public ValueTask DisposeAsync()
    {
        _kubernetes.Dispose();
        return _durableClient.DisposeAsync();
    }

    private async Task<string> StartOrchestrationAsync(int activities, CancellationToken cancellationToken)
    {
        string instanceId = await _durableClient.ScheduleNewOrchestrationInstanceAsync(
            new TaskName("RunAsync"),
            new { ActivityCount = activities, ActivityTime = _options.ActivityDuration },
            cancellationToken).ConfigureAwait(false);

        _logger.StartedOrchestration("RunAsync", instanceId);
        return instanceId;
    }

    private async Task EnsureRunningAsync(string instanceId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        OrchestrationMetadata? metadata = await _durableClient
            .GetInstanceAsync(instanceId, cancellationToken)
            .ConfigureAwait(false);

        Assert.IsTrue(
            metadata is not null && metadata.RuntimeStatus is OrchestrationRuntimeStatus.Pending or OrchestrationRuntimeStatus.Running,
            $"Instance '{instanceId}' has status '{metadata?.RuntimeStatus}'.");
    }

    private async Task<OrchestrationRuntimeStatus?> WaitForOrchestration(string instanceId, CancellationToken cancellationToken)
    {
        OrchestrationMetadata? metadata;

        _logger.WaitingForOrchestration(instanceId);

        while (true)
        {
            await Task.Delay(_options.PollingInterval, cancellationToken).ConfigureAwait(false);

            metadata = await _durableClient
                .GetInstanceAsync(instanceId, cancellationToken)
                .ConfigureAwait(false);

            if (metadata is null || metadata.IsCompleted)
                break;

            _logger.ObservedOrchestrationStatus(instanceId, metadata.RuntimeStatus);
        }

        _logger.ObservedOrchestrationCompletion(instanceId, metadata?.RuntimeStatus);
        return metadata?.RuntimeStatus;
    }

    private async Task WaitForScaleDownAsync(CancellationToken cancellationToken)
    {
        V1Scale scale;

        _logger.MonitoringWorkerScaleDown(_options.MinReplicas);

        do
        {
            // Wait a moment before checking the first time
            await Task.Delay(_options.PollingInterval, cancellationToken).ConfigureAwait(false);

            scale = await _kubernetes.AppsV1
                .ReadNamespacedDeploymentScaleAsync(_deployment.Name, _deployment.Namespace, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            _logger.ObservedKubernetesDeploymentScale(
                _deployment.Name!,
                _deployment.Namespace!,
                scale.Status.Replicas,
                scale.Spec.Replicas.GetValueOrDefault());
        } while (scale.Status.Replicas != _options.MinReplicas || scale.Spec.Replicas.GetValueOrDefault() != _options.MinReplicas);
    }

    private async Task WaitForScaleUpAsync(int min, Func<CancellationToken, Task> onPollAsync, CancellationToken cancellationToken)
    {
        V1Scale scale;

        _logger.MonitoringWorkerScaleUp(min);

        do
        {
            // Wait a moment before checking the first time
            await Task.Delay(_options.PollingInterval, cancellationToken).ConfigureAwait(false);

            // Invoke some delegate with each iteration
            await onPollAsync(cancellationToken).ConfigureAwait(false);

            scale = await _kubernetes.AppsV1
                .ReadNamespacedDeploymentScaleAsync(_deployment.Name, _deployment.Namespace, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            _logger.ObservedKubernetesDeploymentScale(
                _deployment.Name!,
                _deployment.Namespace!,
                scale.Status.Replicas,
                scale.Spec.Replicas.GetValueOrDefault());
        } while (scale.Status.Replicas < min || scale.Spec.Replicas.GetValueOrDefault() < min);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Ignore errors as this is a test clean up.")]
    private async Task<bool> TryTerminateAsync(string instanceId, CancellationToken cancellationToken)
    {
        try
        {
            await _durableClient.TerminateInstanceAsync(instanceId, cancellationToken).ConfigureAwait(false);
            _logger.TerminatedOrchestration(instanceId);
            return true;
        }
        catch (Exception e)
        {
            _logger.FailedTerminatingOrchestration(e, instanceId);
            return false;
        }
    }
}
