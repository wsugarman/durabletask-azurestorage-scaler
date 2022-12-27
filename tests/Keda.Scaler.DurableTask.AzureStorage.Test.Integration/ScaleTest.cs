// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using Keda.Scaler.DurableTask.AzureStorage.Test.Integration.K8s;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.ContextImplementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Integration;

[TestClass]
public sealed class ScaleTest : IDisposable
{
    private readonly ILogger _logger;
    private readonly IKubernetes _kubernetes;
    private readonly IDurableClient _durableClient;
    private readonly FunctionDeploymentOptions _deployment;
    private readonly ScaleTestOptions _options;

    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .AddEnvironmentVariables()
        .Build();

    public ScaleTest()
    {
        IServiceCollection services = new ServiceCollection()
            .AddSingleton(Configuration);

        services
            .AddOptions<DurableTaskOptions>()
            .Bind(Configuration.GetSection("DurableTask"));

        services
            .AddOptions<KubernetesOptions>()
            .Bind(Configuration.GetSection(KubernetesOptions.DefaultSectionName))
            .ValidateDataAnnotations();

        services
            .AddOptions<FunctionDeploymentOptions>()
            .Bind(Configuration.GetSection(FunctionDeploymentOptions.DefaultSectionName))
            .ValidateDataAnnotations();

        services
            .AddOptions<ScaleTestOptions>()
            .Bind(Configuration.GetSection(ScaleTestOptions.DefaultSectionName))
            .ValidateDataAnnotations();

        IServiceProvider provider = services
            .AddLogging(b => b.AddConsole())
            .AddSingleton(
                sp =>
                {
                    KubernetesOptions options = sp.GetRequiredService<IOptions<KubernetesOptions>>().Value;
                    return KubernetesClientConfiguration.BuildConfigFromConfigFile(
                        options.ConfigPath,
                        options.ConfigPath);
                })
            .AddSingleton<IKubernetes>(sp => new Kubernetes(sp.GetRequiredService<KubernetesClientConfiguration>()))
            .AddDurableClientFactory()
            .BuildServiceProvider();

        _logger = provider.GetRequiredService<ILogger<ScaleTest>>();
        _kubernetes = provider.GetRequiredService<IKubernetes>();
        _durableClient = provider.GetRequiredService<IDurableClientFactory>().CreateClient();
        _deployment = provider.GetRequiredService<IOptions<FunctionDeploymentOptions>>().Value;
        _options = provider.GetRequiredService<IOptions<ScaleTestOptions>>().Value;
    }

    [TestMethod]
    public async Task Activities()
    {
        using CancellationTokenSource tokenSource = new CancellationTokenSource();
        tokenSource.CancelAfter(_options.Timeout);

        await WaitForScaleDownAsync(tokenSource.Token).ConfigureAwait(false);

        // Start orchestration with 5 activities
        OrchestrationRuntimeStatus finalStatus;
        string instanceId = await StartOrchestrationAsync(5, TimeSpan.FromMinutes(2)).ConfigureAwait(false);
        try
        {
            // Assert scale
            int expected = GetMinExpectedScale(5);
            await WaitForScaleUpAsync(expected, t => EnsureRunningAsync(instanceId, t), tokenSource.Token).ConfigureAwait(false);

            // Assert completion
            finalStatus = await WaitForOrchestration(instanceId, tokenSource.Token).ConfigureAwait(false);
        }
        catch (Exception)
        {
            string reason = tokenSource.IsCancellationRequested ? "Test timed out." : "Encountered unhandled exception.";
            await _durableClient.TerminateAsync(instanceId, reason).ConfigureAwait(false);
            throw;
        }

        // Ensure it completed successfully
        Assert.AreEqual(OrchestrationRuntimeStatus.Completed, finalStatus);
    }

    [TestMethod]
    public void Orchestrations()
    {
    }

    public void Dispose()
        => _kubernetes.Dispose();

    private Task<string> StartOrchestrationAsync(int activities, TimeSpan duration)
        => _durableClient.StartNewAsync("RunAsync", new { ActivityCount = activities, ActivityTime = duration });

    private async Task EnsureRunningAsync(string instanceId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DurableOrchestrationStatus status = await _durableClient
            .GetStatusAsync(instanceId, showHistory: false, showHistoryOutput: false, showInput: false)
            .ConfigureAwait(false);

        Assert.IsTrue(
            status.RuntimeStatus is OrchestrationRuntimeStatus.Pending or OrchestrationRuntimeStatus.Running,
            $"Instance '{instanceId}' has status '{status.RuntimeStatus}'.");
    }

    private async Task<OrchestrationRuntimeStatus> WaitForOrchestration(string instanceId, CancellationToken cancellationToken)
    {
        DurableOrchestrationStatus status;

        _logger.LogInformation("Waiting for instance '{InstanceId}' to complete.", instanceId);

        do
        {
            await Task.Delay(_options.PollingInterval, cancellationToken).ConfigureAwait(false);

            status = await _durableClient
                .GetStatusAsync(instanceId, showHistory: false, showHistoryOutput: false, showInput: false)
                .ConfigureAwait(false);

            _logger.LogInformation("Current status of instance '{InstanceId}' is '{Status}'.", instanceId, status.RuntimeStatus);
        } while (!status.RuntimeStatus.IsTerminal());

        return status.RuntimeStatus;
    }

    private async Task WaitForScaleDownAsync(CancellationToken cancellationToken)
    {
        V1Scale scale;

        _logger.LogInformation("Waiting for scale down.");

        do
        {
            // Wait a moment before checking the first time
            await Task.Delay(_options.PollingInterval, cancellationToken).ConfigureAwait(false);

            scale = await _kubernetes.AppsV1
                .ReadNamespacedDeploymentScaleAsync(_deployment.Name, _deployment.Namespace, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation("Current scale for deployment '{Deployment}' in namespace '{Namespace}' is {Status}/{Spec}...",
                _deployment.Name,
                _deployment.Namespace,
                scale.Status.Replicas,
                scale.Spec.Replicas);
        } while (scale.Status.Replicas != 0 || scale.Spec.Replicas != 0);
    }

    private async Task WaitForScaleUpAsync(int min, Func<CancellationToken, Task> onPollAsync, CancellationToken cancellationToken = default)
    {
        V1Scale scale;

        _logger.LogInformation("Waiting for at least {Target} replicas.", min);

        do
        {
            // Wait a moment before checking the first time
            await Task.Delay(_options.PollingInterval, cancellationToken).ConfigureAwait(false);

            // Invoke some delegate with each iteration
            await onPollAsync(cancellationToken).ConfigureAwait(false);

            scale = await _kubernetes.AppsV1
                .ReadNamespacedDeploymentScaleAsync(_deployment.Name, _deployment.Namespace, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation("Current scale for deployment '{Deployment}' in namespace '{Namespace}' is {Status}/{Spec}...",
                _deployment.Name,
                _deployment.Namespace,
                scale.Status.Replicas,
                scale.Spec.Replicas);
        } while (scale.Status.Replicas < min || scale.Spec.Replicas < min);
    }

    private int GetMinExpectedScale(int activities)
    {
        // Note: We don't want to take too firm of a dependency on how the orchestrations are distributed
        //       between the partitions, so we'll instead simply assert a "minimum scale" based on the
        //       number of activity work items in the queue

        // Min = (Activities + MinOrchestrations*MaxActivitiesPerWorker)/MaxActivitiesPerWorker
        return (activities + _options.MaxActivitiesPerWorker) / _options.MaxActivitiesPerWorker;
    }
}
