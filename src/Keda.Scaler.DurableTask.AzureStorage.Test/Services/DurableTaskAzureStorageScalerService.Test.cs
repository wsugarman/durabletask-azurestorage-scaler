// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Keda.Scaler.DurableTask.AzureStorage.Kubernetes;
using Keda.Scaler.DurableTask.AzureStorage.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Services;

[TestClass]
public class DurableTaskAzureStorageScalerServiceTest
{
    private const string MetricName = "TestMetric";

    private readonly Mock<IDurableTaskAzureStorageScaler> _mockScaler = new Mock<IDurableTaskAzureStorageScaler>();
    private readonly IServiceProvider _serviceProvider;

    public DurableTaskAzureStorageScalerServiceTest()
    {
        _mockScaler.Setup(s => s.MetricName).Returns(MetricName);

        ServiceCollection services = new ServiceCollection();
        services.AddSingleton(_mockScaler.Object);
        _serviceProvider = services.BuildServiceProvider();
    }

    [TestMethod]
    public void CtorExceptions()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new DurableTaskAzureStorageScalerService(null!));
        Assert.ThrowsException<InvalidOperationException>(() => new DurableTaskAzureStorageScalerService(new ServiceCollection().BuildServiceProvider()));
    }

    [TestMethod]
    public async Task IsActive()
    {
        ScalerMetadata metadata = new ScalerMetadata
        {
            AccountName = "unitteststorage",
            Cloud = nameof(CloudEnvironment.AzurePublicCloud),
            MaxMessageLatencyMilliseconds = 500,
            ScaleIncrement = 2,
            TaskHubName = "UnitTestTaskHub",
            UseAAdPodIdentity = true,
        };

        using CancellationTokenSource tokenSource = new CancellationTokenSource();

        ScaledObjectRef scaledObj = CreateScaledObjectRef(metadata);
        ServerCallContext context = new MockServerCallContext(tokenSource.Token);
        _mockScaler
            .Setup(s => s.IsActiveAsync(It.Is(metadata, ScalerMetadataEqualityComparer.Instance), tokenSource.Token))
            .ReturnsAsync(true);

        DurableTaskAzureStorageScalerService service = new DurableTaskAzureStorageScalerService(_serviceProvider);
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => service.IsActive(null!, context)).ConfigureAwait(false);
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => service.IsActive(scaledObj, null!)).ConfigureAwait(false);
        IsActiveResponse actual = await service.IsActive(scaledObj, context).ConfigureAwait(false);
        Assert.IsTrue(actual.Result);
    }

    [TestMethod]
    public async Task GetMetricSpec()
    {
        const long targetValue = 42;

        ScalerMetadata metadata = new ScalerMetadata
        {
            AccountName = "unitteststorage",
            Cloud = nameof(CloudEnvironment.AzurePublicCloud),
            MaxMessageLatencyMilliseconds = 500,
            ScaleIncrement = 2,
            TaskHubName = "UnitTestTaskHub",
            UseAAdPodIdentity = true,
        };

        using CancellationTokenSource tokenSource = new CancellationTokenSource();

        ScaledObjectRef scaledObj = CreateScaledObjectRef(metadata);
        ServerCallContext context = new MockServerCallContext(tokenSource.Token);
        _mockScaler
            .Setup(s => s.GetMetricSpecAsync(It.Is(metadata, ScalerMetadataEqualityComparer.Instance), tokenSource.Token))
            .ReturnsAsync(targetValue);

        DurableTaskAzureStorageScalerService service = new DurableTaskAzureStorageScalerService(_serviceProvider);

        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => service.GetMetricSpec(null!, context)).ConfigureAwait(false);
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => service.GetMetricSpec(scaledObj, null!)).ConfigureAwait(false);
        GetMetricSpecResponse response = await service.GetMetricSpec(scaledObj, context).ConfigureAwait(false);

        MetricSpec actual = response.MetricSpecs.Single();
        Assert.AreEqual(MetricName, actual.MetricName);
        Assert.AreEqual(targetValue, actual.TargetSize);
    }

    [TestMethod]
    public async Task GetMetrics()
    {
        const long metricValue = 17;

        ScaledObjectReference deployment = new ScaledObjectReference("unit-test-func", "durable-task");
        ScalerMetadata metadata = new ScalerMetadata
        {
            AccountName = "unitteststorage",
            Cloud = nameof(CloudEnvironment.AzurePublicCloud),
            MaxMessageLatencyMilliseconds = 500,
            ScaleIncrement = 2,
            TaskHubName = "UnitTestTaskHub",
            UseAAdPodIdentity = true,
        };

        using CancellationTokenSource tokenSource = new CancellationTokenSource();

        GetMetricsRequest request = CreateGetMetricsRequest(deployment, metadata);
        ServerCallContext context = new MockServerCallContext(tokenSource.Token);
        _mockScaler
            .Setup(s => s.GetMetricValueAsync(deployment, It.Is(metadata, ScalerMetadataEqualityComparer.Instance), tokenSource.Token))
            .ReturnsAsync(metricValue);

        DurableTaskAzureStorageScalerService service = new DurableTaskAzureStorageScalerService(_serviceProvider);

        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => service.GetMetrics(null!, context)).ConfigureAwait(false);
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => service.GetMetrics(request, null!)).ConfigureAwait(false);
        GetMetricsResponse response = await service.GetMetrics(request, context).ConfigureAwait(false);

        MetricValue actual = response.MetricValues.Single();
        Assert.AreEqual(MetricName, actual.MetricName);
        Assert.AreEqual(metricValue, actual.MetricValue_);
    }

    private static ScaledObjectRef CreateScaledObjectRef(ScalerMetadata metadata)
        => CreateScaledObjectRef(default, metadata);

    private static GetMetricsRequest CreateGetMetricsRequest(ScaledObjectReference deployment, ScalerMetadata metadata)
        => new GetMetricsRequest { MetricName = MetricName, ScaledObjectRef = CreateScaledObjectRef(deployment, metadata) };

    private static ScaledObjectRef CreateScaledObjectRef(ScaledObjectReference deployment, ScalerMetadata metadata)
    {
        ScaledObjectRef result = deployment == default
            ? new ScaledObjectRef()
            : new ScaledObjectRef
            {
                Name = deployment.Name,
                Namespace = deployment.Namespace,
            };

        if (metadata.AccountName is not null)
            result.ScalerMetadata[nameof(ScalerMetadata.AccountName)] = metadata.AccountName;

        if (metadata.Connection is not null)
            result.ScalerMetadata[nameof(ScalerMetadata.Connection)] = metadata.Connection;

        if (metadata.ConnectionFromEnv is not null)
            result.ScalerMetadata[nameof(ScalerMetadata.ConnectionFromEnv)] = metadata.ConnectionFromEnv;

        if (metadata.TaskHubName is not null)
            result.ScalerMetadata[nameof(ScalerMetadata.TaskHubName)] = metadata.TaskHubName;

        result.ScalerMetadata[nameof(ScalerMetadata.Cloud)] = metadata.Cloud;
        result.ScalerMetadata[nameof(ScalerMetadata.MaxMessageLatencyMilliseconds)] = metadata.MaxMessageLatencyMilliseconds.ToString(CultureInfo.InvariantCulture);
        result.ScalerMetadata[nameof(ScalerMetadata.ScaleIncrement)] = metadata.ScaleIncrement.ToString(CultureInfo.InvariantCulture);
        result.ScalerMetadata[nameof(ScalerMetadata.UseAAdPodIdentity)] = metadata.UseAAdPodIdentity.ToString(CultureInfo.InvariantCulture);

        return result;
    }
}
