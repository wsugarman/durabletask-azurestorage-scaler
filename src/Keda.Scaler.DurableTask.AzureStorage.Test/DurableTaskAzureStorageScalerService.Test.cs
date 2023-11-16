// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Grpc.Core;
using Keda.Scaler.DurableTask.AzureStorage.Accounts;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Keda.Scaler.DurableTask.AzureStorage.Common;
using Keda.Scaler.DurableTask.AzureStorage.Extensions;
using Keda.Scaler.DurableTask.AzureStorage.TaskHub;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Keda.Scaler.DurableTask.AzureStorage.Test;

[TestClass]
public class DurableTaskAzureStorageScalerServiceTest
{
    private readonly MockEnvironment _environment = new();
    private readonly Mock<AzureStorageTaskHubBrowser> _mockBrowser = new(
        MockBehavior.Strict,
        new Mock<IStorageAccountClientFactory<BlobServiceClient>>(MockBehavior.Strict).Object,
        new Mock<IStorageAccountClientFactory<QueueServiceClient>>(MockBehavior.Strict).Object,
        NullLoggerFactory.Instance);
    private readonly Mock<ITaskHubQueueMonitor> _mockMonitor = new(MockBehavior.Strict);
    private readonly Mock<IOrchestrationAllocator> _mockAllocator = new(MockBehavior.Strict);
    private readonly IServiceProvider _serviceProvider;
    private readonly DurableTaskAzureStorageScalerService _service;

    public DurableTaskAzureStorageScalerServiceTest()
    {
        _serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSingleton(_mockBrowser.Object)
            .AddSingleton(_mockAllocator.Object)
            .AddSingleton<IProcessEnvironment>(_environment)
            .BuildServiceProvider();

        _service = new DurableTaskAzureStorageScalerService(_serviceProvider);
    }

    [TestMethod]
    public void CtorExceptions()
    {
        _ = Assert.ThrowsException<ArgumentNullException>(() => new DurableTaskAzureStorageScalerService(null!));
        _ = Assert.ThrowsException<InvalidOperationException>(() => new DurableTaskAzureStorageScalerService(new ServiceCollection().BuildServiceProvider()));
    }

    [TestMethod]
    public async Task GetMetrics()
    {
        using CancellationTokenSource tokenSource = new();

        ScalerMetadata metadata = new()
        {
            AccountName = "unitteststorage",
            Cloud = nameof(CloudEnvironment.AzurePublicCloud),
            MaxActivitiesPerWorker = 5,
            MaxOrchestrationsPerWorker = 3,
            TaskHubName = "UnitTestTaskHub",
            UseManagedIdentity = true,
        };
        AzureStorageAccountInfo accountInfo = metadata.GetAccountInfo(_environment);
        TaskHubQueueUsage usage = new([1, 2, 3], 10);

        GetMetricsRequest request = CreateGetMetricsRequest(metadata);
        ServerCallContext context = new MockServerCallContext(tokenSource.Token);

        // Argument validation
        _ = await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _service.GetMetrics(null!, context)).ConfigureAwait(false);
        _ = await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _service.GetMetrics(request, null!)).ConfigureAwait(false);

        // Set up
        _ = _mockBrowser
            .Setup(s => s
                .GetMonitorAsync(
                    It.Is(accountInfo, AzureStorageAccountInfoEqualityComparer.Instance),
                    "UnitTestTaskHub",
                    tokenSource.Token))
            .Returns(() => ValueTask.FromResult(_mockMonitor.Object));
        _ = _mockMonitor
            .Setup(m => m.GetUsageAsync(tokenSource.Token))
            .Returns(() => ValueTask.FromResult(usage));
        _ = _mockAllocator
            .Setup(a => a.GetWorkerCount(usage.ControlQueueMessages, 3))
            .Returns(2);

        // Run test
        GetMetricsResponse response = await _service.GetMetrics(request, context).ConfigureAwait(false);

        MetricValue actual = response.MetricValues.Single();
        Assert.AreEqual(DurableTaskAzureStorageScalerService.MetricName, actual.MetricName);
        Assert.AreEqual(10 + (2 * 5), actual.MetricValue_);
    }

    [TestMethod]
    public async Task GetMetricSpec()
    {
        ScalerMetadata metadata = new()
        {
            AccountName = "unitteststorage",
            Cloud = nameof(CloudEnvironment.AzurePublicCloud),
            MaxActivitiesPerWorker = 5,
            MaxOrchestrationsPerWorker = 2,
            TaskHubName = "UnitTestTaskHub",
            UseManagedIdentity = true,
        };

        using CancellationTokenSource tokenSource = new();

        ScaledObjectRef scaledObj = CreateScaledObjectRef(metadata);
        ServerCallContext context = new MockServerCallContext(tokenSource.Token);
        DurableTaskAzureStorageScalerService service = new(_serviceProvider);

        _ = await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => service.GetMetricSpec(null!, context)).ConfigureAwait(false);
        _ = await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => service.GetMetricSpec(scaledObj, null!)).ConfigureAwait(false);
        GetMetricSpecResponse response = await service.GetMetricSpec(scaledObj, context).ConfigureAwait(false);

        MetricSpec actual = response.MetricSpecs.Single();
        Assert.AreEqual(DurableTaskAzureStorageScalerService.MetricName, actual.MetricName);
        Assert.AreEqual(5, actual.TargetSize);
    }

    [TestMethod]
    public async Task IsActive()
    {
        using CancellationTokenSource tokenSource = new();

        ScalerMetadata metadata = new()
        {
            AccountName = "unitteststorage",
            Cloud = nameof(CloudEnvironment.AzurePublicCloud),
            MaxActivitiesPerWorker = 5,
            MaxOrchestrationsPerWorker = 3,
            TaskHubName = "UnitTestTaskHub",
            UseManagedIdentity = true,
        };
        AzureStorageAccountInfo accountInfo = metadata.GetAccountInfo(_environment);
        TaskHubQueueUsage usage = new([1, 2, 3], 10);

        ScaledObjectRef scaledObjectRef = CreateScaledObjectRef(metadata);
        ServerCallContext context = new MockServerCallContext(tokenSource.Token);

        // Argument validation
        _ = await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _service.IsActive(null!, context)).ConfigureAwait(false);
        _ = await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _service.IsActive(scaledObjectRef, null!)).ConfigureAwait(false);

        // Set up
        _ = _mockBrowser
            .Setup(s => s
                .GetMonitorAsync(
                    It.Is(accountInfo, AzureStorageAccountInfoEqualityComparer.Instance),
                    "UnitTestTaskHub",
                    tokenSource.Token))
            .Returns(() => ValueTask.FromResult(_mockMonitor.Object));
        _ = _mockMonitor
            .Setup(m => m.GetUsageAsync(tokenSource.Token))
            .Returns(() => ValueTask.FromResult(usage));
        _ = _mockAllocator
            .Setup(a => a.GetWorkerCount(usage.ControlQueueMessages, 3))
            .Returns(2);

        // Run test (with activity)
        IsActiveResponse response = await _service.IsActive(scaledObjectRef, context).ConfigureAwait(false);
        Assert.IsTrue(response.Result);

        // Run test (without activity)
        _mockMonitor.Reset();
        _mockAllocator.Reset();
        _ = _mockMonitor
            .Setup(m => m.GetUsageAsync(tokenSource.Token))
            .Returns(() => ValueTask.FromResult(new TaskHubQueueUsage(Array.Empty<int>(), 0)));
        _ = _mockAllocator
            .Setup(a => a.GetWorkerCount(Array.Empty<int>(), 3))
            .Returns(0);

        response = await _service.IsActive(scaledObjectRef, context).ConfigureAwait(false);
        Assert.IsFalse(response.Result);
    }

    private static GetMetricsRequest CreateGetMetricsRequest(ScalerMetadata metadata)
        => new()
        {
            MetricName = DurableTaskAzureStorageScalerService.MetricName,
            ScaledObjectRef = CreateScaledObjectRef(metadata),
        };

    private static ScaledObjectRef CreateScaledObjectRef(ScalerMetadata metadata)
    {
        ScaledObjectRef result = new();

        if (metadata.AccountName is not null)
            result.ScalerMetadata[nameof(ScalerMetadata.AccountName)] = metadata.AccountName;

        if (metadata.Connection is not null)
            result.ScalerMetadata[nameof(ScalerMetadata.Connection)] = metadata.Connection;

        if (metadata.ConnectionFromEnv is not null)
            result.ScalerMetadata[nameof(ScalerMetadata.ConnectionFromEnv)] = metadata.ConnectionFromEnv;

        if (metadata.TaskHubName is not null)
            result.ScalerMetadata[nameof(ScalerMetadata.TaskHubName)] = metadata.TaskHubName;

        result.ScalerMetadata[nameof(ScalerMetadata.Cloud)] = metadata.Cloud;
        result.ScalerMetadata[nameof(ScalerMetadata.MaxActivitiesPerWorker)] = metadata.MaxActivitiesPerWorker.ToString(CultureInfo.InvariantCulture);
        result.ScalerMetadata[nameof(ScalerMetadata.MaxOrchestrationsPerWorker)] = metadata.MaxOrchestrationsPerWorker.ToString(CultureInfo.InvariantCulture);
        result.ScalerMetadata[nameof(ScalerMetadata.UseManagedIdentity)] = metadata.UseManagedIdentity.ToString(CultureInfo.InvariantCulture);

        return result;
    }
}
