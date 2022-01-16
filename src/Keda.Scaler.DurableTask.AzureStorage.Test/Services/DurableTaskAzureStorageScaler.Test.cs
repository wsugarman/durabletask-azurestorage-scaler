// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.AzureStorage.Monitoring;
using k8s;
using k8s.Models;
using Keda.Scaler.DurableTask.AzureStorage.Common;
using Keda.Scaler.DurableTask.AzureStorage.Kubernetes;
using Keda.Scaler.DurableTask.AzureStorage.Provider;
using Keda.Scaler.DurableTask.AzureStorage.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Rest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using ScalerKubernetesExtensions = Keda.Scaler.DurableTask.AzureStorage.Extensions.KubernetesExtensions;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Services;

[TestClass]
public class DurableTaskAzureStorageScalerTest
{
    [TestMethod]
    public void MetricNameProperty()
    {
        DurableTaskAzureStorageScaler scaler = new DurableTaskAzureStorageScaler(
            Mock.Of<IKubernetes>(),
            Mock.Of<IPerformanceMonitorFactory>(),
            NullLoggerFactory.Instance);

        Assert.AreEqual("WorkerDemand", scaler.MetricName);
    }

    [TestMethod]
    public void CtorExceptions()
    {
        IKubernetes kubernetes = Mock.Of<IKubernetes>();
        IPerformanceMonitorFactory monitorFactory = Mock.Of<IPerformanceMonitorFactory>();
        ILoggerFactory loggerFactory = NullLoggerFactory.Instance;

        Assert.ThrowsException<ArgumentNullException>(() => new DurableTaskAzureStorageScaler(null!, monitorFactory, loggerFactory));
        Assert.ThrowsException<ArgumentNullException>(() => new DurableTaskAzureStorageScaler(kubernetes, null!, loggerFactory));
        Assert.ThrowsException<ArgumentNullException>(() => new DurableTaskAzureStorageScaler(kubernetes, monitorFactory, null!));
    }

    [TestMethod]
    public async Task GetMetricSpecAsync()
    {
        IKubernetes kubernetes = Mock.Of<IKubernetes>();
        IPerformanceMonitorFactory monitorFactory = Mock.Of<IPerformanceMonitorFactory>();
        ILoggerFactory loggerFactory = NullLoggerFactory.Instance;

        using CancellationTokenSource tokenSource = new CancellationTokenSource();

        DurableTaskAzureStorageScaler scaler = new DurableTaskAzureStorageScaler(kubernetes, monitorFactory, loggerFactory);

        // Null metadata
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => scaler.GetMetricSpecAsync(null!, tokenSource.Token).AsTask()).ConfigureAwait(false);

        // Non-null metadata
        Assert.AreEqual(
            DurableTaskAzureStorageScaler.MetricSpecValue,
            await scaler.GetMetricSpecAsync(new ScalerMetadata(), tokenSource.Token).ConfigureAwait(false));
    }

    [DataTestMethod]
    [DataRow(ScaleAction.AddWorker, 2, 5, DurableTaskAzureStorageScaler.MetricSpecValue * 2 * 5, null, null)]
    [DataRow(ScaleAction.AddWorker, 2, null, DurableTaskAzureStorageScaler.MetricSpecValue * 2, null, null)]
    [DataRow(ScaleAction.RemoveWorker, 2, 5, DurableTaskAzureStorageScaler.MetricSpecValue / 2 * 5, null, "Deployment")]
    [DataRow(ScaleAction.RemoveWorker, 2, 0, DurableTaskAzureStorageScaler.MetricSpecValue, null, "StatefulSet")]
    [DataRow(ScaleAction.None, 2, 5, DurableTaskAzureStorageScaler.MetricSpecValue * 5, "apps/v1", null)]
    [DataRow(ScaleAction.None, 2, null, DurableTaskAzureStorageScaler.MetricSpecValue, "apps/v2", null)]
    [DataRow(null, 2, 5, DurableTaskAzureStorageScaler.MetricSpecValue * 5, "apps/v1", "StatefulSet")]
    [DataRow((ScaleAction)123, 2, 5, 0, "custom.sh/v2beta", "beehive")]
    [DataRow((ScaleAction)456, 2, 0, 0, "custom.sh/v3beta", "gaggle")]
    public async Task GetMetricValueAsync(
        ScaleAction? action,
        double ratio,
        int? replicas,
        long expectedMetric,
        string? apiVersion,
        string? kind)
    {
        // Create input
        ScaledObjectReference scaledObject = new ScaledObjectReference("unit-test-func", "durable-task");
        ScalerMetadata metadata = new ScalerMetadata
        {
            AccountName = "unitteststorage",
            MaxMessageLatencyMilliseconds = 1000,
            ScaleIncrement = ratio,
            TaskHubName = "UnitTestTaskHub",
            UseAAdPodIdentity = true,
        };

        using CancellationTokenSource tokenSource = new CancellationTokenSource();

        // Create mock services
        V1ScaleTargetRef scaledTarget = new V1ScaleTargetRef
        {
            ApiVersion = apiVersion,
            Kind = kind,
            Name = "unit-test",
        };
        V1ScaleStatus? scaleStatus = replicas is null ? null : new V1ScaleStatus { Replicas = replicas.Value };
        IKubernetes k8s = CreateMockKubernetesClient(scaledObject, scaledTarget, scaleStatus, tokenSource.Token);

        PerformanceHeartbeat? heartbeat = action is null ? null : CreateHeartbeat(action.GetValueOrDefault());
        Mock<IPerformanceMonitor> monitorMock = new Mock<IPerformanceMonitor>(MockBehavior.Strict);
        monitorMock
            .Setup(m => m.GetHeartbeatAsync(replicas ?? 0))
            .ReturnsAsync(heartbeat);
        monitorMock
            .Setup(m => m.Dispose());

        Mock<IPerformanceMonitorFactory> factoryMock = new Mock<IPerformanceMonitorFactory>(MockBehavior.Strict);
        factoryMock
            .Setup(f => f.CreateAsync(metadata, tokenSource.Token))
            .ReturnsAsync(monitorMock.Object);

        IProcessEnvironment environment = CurrentEnvironment.Instance;
        ILoggerFactory loggerFactory = NullLoggerFactory.Instance;

        DurableTaskAzureStorageScaler scaler = new DurableTaskAzureStorageScaler(k8s, factoryMock.Object, loggerFactory);

        // Null metadata
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => scaler.GetMetricValueAsync(default, null!).AsTask()).ConfigureAwait(false);

        // Non-null metadata
        Task<long> metricTask = scaler.GetMetricValueAsync(scaledObject, metadata, tokenSource.Token).AsTask();
        if (Enum.IsDefined(action.GetValueOrDefault()))
            Assert.AreEqual(expectedMetric, await metricTask.ConfigureAwait(false));
        else
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => metricTask).ConfigureAwait(false);
    }

    [DataTestMethod]
    [DataRow(ScalerActivity.Null)]
    [DataRow(ScalerActivity.None)]
    [DataRow(ScalerActivity.Some)]
    public async Task IsActiveAsync(ScalerActivity activity)
    {
        // Create input
        ScalerMetadata metadata = new ScalerMetadata
        {
            AccountName = "unitteststorage",
            MaxMessageLatencyMilliseconds = 1000,
            TaskHubName = "UnitTestTaskHub",
            UseAAdPodIdentity = true,
        };

        using CancellationTokenSource tokenSource = new CancellationTokenSource();

        // Create mock services
        PerformanceHeartbeat? heartbeat = activity switch
        {
            ScalerActivity.Null => null,
            ScalerActivity.None => new PerformanceHeartbeat
            {
                ControlQueueLatencies = new TimeSpan[] { TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero },
                WorkItemQueueLatency = TimeSpan.Zero,
            },
            ScalerActivity.Some => new PerformanceHeartbeat
            {
                ControlQueueLatencies = new TimeSpan[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1) },
                WorkItemQueueLatency = TimeSpan.FromMilliseconds(300),
            },
            _ => throw new InvalidOperationException("Unknown scaler activity"),
        };

        Mock<IPerformanceMonitor> monitorMock = new Mock<IPerformanceMonitor>(MockBehavior.Strict);
        monitorMock
            .Setup(m => m.GetHeartbeatAsync(null))
            .ReturnsAsync(heartbeat);
        monitorMock
            .Setup(m => m.Dispose());

        Mock<IPerformanceMonitorFactory> factoryMock = new Mock<IPerformanceMonitorFactory>(MockBehavior.Strict);
        factoryMock
            .Setup(f => f.CreateAsync(metadata, tokenSource.Token))
            .ReturnsAsync(monitorMock.Object);

        IProcessEnvironment environment = CurrentEnvironment.Instance;
        ILoggerFactory loggerFactory = NullLoggerFactory.Instance;

        DurableTaskAzureStorageScaler scaler = new DurableTaskAzureStorageScaler(Mock.Of<IKubernetes>(), factoryMock.Object, loggerFactory);

        // Null metadata
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => scaler.IsActiveAsync(null!, tokenSource.Token).AsTask()).ConfigureAwait(false);

        // Non-null metadata
        Assert.AreEqual(activity == ScalerActivity.Some, await scaler.IsActiveAsync(metadata, tokenSource.Token).ConfigureAwait(false));
    }

    private static PerformanceHeartbeat CreateHeartbeat(ScaleAction action, string? reason = null)
    {
        MethodInfo method = typeof(PerformanceHeartbeat)
            .GetProperty(nameof(PerformanceHeartbeat.ScaleRecommendation))!
            .GetSetMethod(nonPublic: true)!;

        ConstructorInfo ctor = typeof(ScaleRecommendation)
            .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, new Type[] { typeof(ScaleAction), typeof(bool), typeof(string) })!;

        PerformanceHeartbeat heartbeat = new PerformanceHeartbeat();
        method.Invoke(heartbeat, new object[] { ctor.Invoke(new object?[] { action, false, reason }) });
        return heartbeat;
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Kubernetes.Client APIs will dipose of the encapsulating HttpOperationResponse<T>")]
    private static IKubernetes CreateMockKubernetesClient(
        ScaledObjectReference scaledObject,
        V1ScaleTargetRef scaleTarget,
        V1ScaleStatus? scaleStatus,
        CancellationToken cancellationToken)
    {
        Mock<IKubernetes> k8sMock = new Mock<IKubernetes>(MockBehavior.Strict);

        // Setup mock behavior for fetching the ScaledObject
        k8sMock
            .Setup(k => k.GetNamespacedCustomObjectWithHttpMessagesAsync(
                "keda.sh",
                "v1alpha1",
                scaledObject.Namespace,
                "ScaledObjects",
                scaledObject.Name,
                null,
                cancellationToken))
            .ReturnsAsync(
                new HttpOperationResponse<object>
                {
                    Body = JsonSerializer.Deserialize<object>(
                        JsonSerializer.Serialize(
                            new V1ScaledObject
                            {
                                ApiVersion = "keda.sh/v1alpha1",
                                Kind = "ScaledObject",
                                Metadata = new V1ObjectMeta
                                {
                                    Name = scaledObject.Name,
                                    NamespaceProperty = scaledObject.Namespace,
                                },
                                Spec = new V1ScaledObjectSpec
                                {
                                    ScaleTargetRef = scaleTarget,
                                },
                            },
                            ScalerKubernetesExtensions.JsonSerializerOptions),
                        ScalerKubernetesExtensions.JsonSerializerOptions)!
                });

        // Setup mock behavior for fetching the referenced resource scale
        (string? group, string? version) = scaleTarget.ApiGroupAndVersion();
        k8sMock
            .Setup(k => k.GetNamespacedCustomObjectScaleWithHttpMessagesAsync(
                group ?? "apps",
                version ?? "v1",
                scaledObject.Namespace,
                scaleTarget.Kind == null ? "Deployments" : scaleTarget.Kind + 's',
                scaleTarget.Name,
                null,
                cancellationToken))
            .ReturnsAsync(
                new HttpOperationResponse<object>
                {
                    Body = JsonSerializer.Deserialize<object>(
                        JsonSerializer.Serialize(
                            new V1Scale
                            {
                                ApiVersion = scaleTarget.ApiVersion ?? "apps/v1",
                                Kind = scaleTarget.Kind ?? "Deployment",
                                Metadata = new V1ObjectMeta
                                {
                                    Name = scaleTarget.Name,
                                    NamespaceProperty = scaledObject.Namespace,
                                },
                                Status = scaleStatus,
                            },
                            ScalerKubernetesExtensions.JsonSerializerOptions),
                        ScalerKubernetesExtensions.JsonSerializerOptions)!
                });

        return k8sMock.Object;
    }

    public enum ScalerActivity
    {
        Null,
        None,
        Some
    }
}
