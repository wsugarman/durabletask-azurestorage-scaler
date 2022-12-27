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
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Integration;

[TestClass]
public sealed class ScaleTest : IDisposable
{
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

        await WaitForScaleAsync(0, tokenSource.Token).ConfigureAwait(false);

        // Start orchestration with 5 activities
        string instanceId = await StartOrchestrationAsync(5, TimeSpan.FromMinutes(2)).ConfigureAwait(false);
        try
        {

            // Assert scale up to ___
            await WaitForScaleAsync(10, tokenSource.Token).ConfigureAwait(false);

            // Assert completion
            OrchestrationRuntimeStatus finalStatus = await WaitForOrchestration(instanceId, tokenSource.Token).ConfigureAwait(false);
            Assert.AreEqual(OrchestrationRuntimeStatus.Completed, finalStatus);
        }
        catch (Exception)
        {
            string reason = tokenSource.IsCancellationRequested ? "Test timed out." : "Encountered unhandled exception.";
            await _durableClient.TerminateAsync(instanceId, reason).ConfigureAwait(false);
            throw;
        }
    }

    [TestMethod]
    public void Orchestrations()
    {
    }

    public void Dispose()
        => _kubernetes.Dispose();

    private Task<string> StartOrchestrationAsync(int activities, TimeSpan duration)
        => _durableClient.StartNewAsync("RunAsync", new { ActivityCount = activities, ActivityTime = duration });

    private async Task<OrchestrationRuntimeStatus> WaitForOrchestration(string instanceId, CancellationToken cancellationToken)
    {
        while (true)
        {
            DurableOrchestrationStatus status = await _durableClient
                .GetStatusAsync(instanceId, showInput: false)
                .ConfigureAwait(false);

            if (status.RuntimeStatus
                is OrchestrationRuntimeStatus.Completed
                or OrchestrationRuntimeStatus.Canceled
                or OrchestrationRuntimeStatus.Terminated
                or OrchestrationRuntimeStatus.Failed)
            {
                return status.RuntimeStatus;
            }

            await Task.Delay(_options.PollingInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task WaitForScaleAsync(int target, CancellationToken cancellationToken)
    {
        while (true)
        {
            V1Scale scale = await _kubernetes.AppsV1
                .ReadNamespacedDeploymentScaleAsync(_deployment.Name, _deployment.Namespace, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (scale.Spec.Replicas != target || scale.Status.Replicas != target)
                break;

            await Task.Delay(_options.PollingInterval, cancellationToken).ConfigureAwait(false);
        }
    }
}
