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
using Xunit;
using Xunit.Sdk;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Integration;

public sealed class ScaleTest : IAsyncDisposable
{
    private readonly ILogger _logger;
    private readonly IKubernetes _kubernetes;
    private readonly DurableTaskClient _durableClient;
    private readonly FunctionDeploymentOptions _deployment;
    private readonly ScaleTestOptions _options;
    private readonly ServiceProvider _serviceProvider;

    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .AddEnvironmentVariables()
        .Build();

    public ScaleTest(ITestOutputHelper outputHelper)
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

        _serviceProvider = services
            .AddLogging(b => b.AddXUnit(outputHelper))
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

    [Fact]
    public async Task GivenMultipleActivities_WhenRunningOrchestrationInstance_ThenScaleUp()
    {
        const int ExpectedActivityWorkers = 3;
        int activityCount = ExpectedActivityWorkers * _options.MaxActivitiesPerWorker;

        using CancellationTokenSource tokenSource = new();
        tokenSource.CancelAfter(_options.Timeout);

        await WaitForScaleDownAsync(tokenSource.Token);

        // Start 1 orchestration with the configured number of activities
        OrchestrationRuntimeStatus? finalStatus;
        string instanceId = await StartOrchestrationAsync(activityCount, tokenSource.Token);
        try
        {
            // Assert scale (needs at least 3 workers for the activities)
            await WaitForScaleUpAsync(ExpectedActivityWorkers, t => EnsureRunningAsync(instanceId, t), tokenSource.Token);

            // Assert completion
            finalStatus = await WaitForOrchestration(instanceId, tokenSource.Token);
        }
        catch (Exception)
        {
            _ = await TryTerminateAsync(instanceId);
            throw;
        }

        // Ensure it completed successfully
        Assert.Equal(OrchestrationRuntimeStatus.Completed, finalStatus);
    }

    [Fact]
    public async Task GivenMultipleOrchestrationInstances_WhenRunningOrchestrationsConcurrently_ThenScaleUp()
    {
        const int OrchestrationCount = 3;

        using CancellationTokenSource tokenSource = new();
        tokenSource.CancelAfter(_options.Timeout);

        await WaitForScaleDownAsync(tokenSource.Token);

        // Start 3 orchestrations with the configured number of activities
        OrchestrationRuntimeStatus?[] finalStatuses;
        string[] instanceIds = await Task.WhenAll(Enumerable
            .Repeat(_options, OrchestrationCount)
            .Select(o => StartOrchestrationAsync(o.MaxActivitiesPerWorker, tokenSource.Token)));

        try
        {
            // Assert scale (needs at least 3 workers as each orchestration will spawn the max number of activities per worker)
            await WaitForScaleUpAsync(
                OrchestrationCount,
                t => Task.WhenAll(instanceIds.Select(id => EnsureRunningAsync(id, t))),
                tokenSource.Token);

            // Assert completion
            finalStatuses = await Task.WhenAll(instanceIds.Select(id => WaitForOrchestration(id, tokenSource.Token)));
        }
        catch (Exception e) when (e is not IAssertionException)
        {
            _ = await Task.WhenAll(instanceIds.Select(TryTerminateAsync));
            throw;
        }

        // Ensure it completed successfully
        foreach (OrchestrationRuntimeStatus? actual in finalStatuses)
            Assert.Equal(OrchestrationRuntimeStatus.Completed, actual);
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

        Assert.NotNull(metadata);
        Assert.True(
            metadata.RuntimeStatus is OrchestrationRuntimeStatus.Pending or OrchestrationRuntimeStatus.Running,
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
                _logger.ObservedKubernetesDeploymentScale(
                    _deployment.Name!,
                    _deployment.Namespace!,
                    scale.Status.Replicas,
                    scale.Spec.Replicas.GetValueOrDefault());
            }
        } while (scale.Status.Replicas < min || scale.Spec.Replicas.GetValueOrDefault() < min);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Ignore errors as this is a test clean up.")]
    private async Task<bool> TryTerminateAsync(string instanceId)
    {
        try
        {
            await _durableClient.TerminateInstanceAsync(instanceId, CancellationToken.None).ConfigureAwait(false);
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
