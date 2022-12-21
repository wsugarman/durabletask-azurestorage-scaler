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
    private readonly MockEnvironment _environment = new MockEnvironment();
    private readonly Mock<AzureStorageTaskHubBrowser> _mockBrowser = new Mock<AzureStorageTaskHubBrowser>(
        MockBehavior.Strict,
        new Mock<IStorageAccountClientFactory<BlobServiceClient>>(MockBehavior.Strict).Object,
        new Mock<IStorageAccountClientFactory<QueueServiceClient>>(MockBehavior.Strict).Object,
        NullLoggerFactory.Instance);
    private readonly Mock<ITaskHubQueueMonitor> _mockMonitor = new Mock<ITaskHubQueueMonitor>(MockBehavior.Strict);
    private readonly Mock<IOrchestrationAllocator> _mockAllocator = new Mock<IOrchestrationAllocator>(MockBehavior.Strict);
    private readonly IServiceProvider _serviceProvider;
    private readonly DurableTaskAzureStorageScalerService _service;

    public DurableTaskAzureStorageScalerServiceTest()
    {
        _serviceProvider = new ServiceCollection()
            .AddSingleton(_mockBrowser.Object)
            .AddSingleton(_mockAllocator.Object)
            .AddSingleton<IProcessEnvironment>(_environment)
            .BuildServiceProvider();

        _service = new DurableTaskAzureStorageScalerService(_serviceProvider);
    }

    [TestMethod]
    public void CtorExceptions()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new DurableTaskAzureStorageScalerService(null!));
        Assert.ThrowsException<InvalidOperationException>(() => new DurableTaskAzureStorageScalerService(new ServiceCollection().BuildServiceProvider()));
    }

    [TestMethod]
    public async Task GetMetrics()
    {
        using CancellationTokenSource tokenSource = new CancellationTokenSource();

        ScalerMetadata metadata = new ScalerMetadata
        {
            AccountName = "unitteststorage",
            Cloud = nameof(CloudEnvironment.AzurePublicCloud),
            MaxActivitiesPerWorker = 5,
            MaxOrchestrationsPerWorker = 3,
            TaskHubName = "UnitTestTaskHub",
            UseManagedIdentity = true,
        };
        AzureStorageAccountInfo accountInfo = metadata.GetAccountInfo(_environment);
        TaskHubQueueUsage usage = new TaskHubQueueUsage(new int[] { 1, 2, 3 }, 10);

        GetMetricsRequest request = CreateGetMetricsRequest(metadata);
        ServerCallContext context = new MockServerCallContext(tokenSource.Token);

        // Argument validation
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _service.GetMetrics(null!, context)).ConfigureAwait(false);
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _service.GetMetrics(request, null!)).ConfigureAwait(false);

        // Set up
        _mockBrowser
            .Setup(s => s
                .GetMonitorAsync(
                    It.Is(accountInfo, AzureStorageAccountInfoEqualityComparer.Instance),
                    "UnitTestTaskHub",
                    tokenSource.Token))
            .Returns(() => ValueTask.FromResult(_mockMonitor.Object));
        _mockMonitor
            .Setup(m => m.GetUsageAsync(tokenSource.Token))
            .Returns(() => ValueTask.FromResult(usage));
        _mockAllocator
            .Setup(a => a.GetWorkerCount(usage.ControlQueueMessages, 3))
            .Returns(2);

        // Run test
        GetMetricsResponse response = await _service.GetMetrics(request, context).ConfigureAwait(false);

        MetricValue actual = response.MetricValues.Single();
        Assert.AreEqual(DurableTaskAzureStorageScalerService.MetricName, actual.MetricName);
        Assert.AreEqual(10 + 2 * 5, actual.MetricValue_);
    }

    [TestMethod]
    public async Task GetMetricSpec()
    {
        ScalerMetadata metadata = new ScalerMetadata
        {
            AccountName = "unitteststorage",
            Cloud = nameof(CloudEnvironment.AzurePublicCloud),
            MaxActivitiesPerWorker = 5,
            MaxOrchestrationsPerWorker = 2,
            TaskHubName = "UnitTestTaskHub",
            UseManagedIdentity = true,
        };

        using CancellationTokenSource tokenSource = new CancellationTokenSource();

        ScaledObjectRef scaledObj = CreateScaledObjectRef(metadata);
        ServerCallContext context = new MockServerCallContext(tokenSource.Token);
        DurableTaskAzureStorageScalerService service = new DurableTaskAzureStorageScalerService(_serviceProvider);

        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => service.GetMetricSpec(null!, context)).ConfigureAwait(false);
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => service.GetMetricSpec(scaledObj, null!)).ConfigureAwait(false);
        GetMetricSpecResponse response = await service.GetMetricSpec(scaledObj, context).ConfigureAwait(false);

        MetricSpec actual = response.MetricSpecs.Single();
        Assert.AreEqual(DurableTaskAzureStorageScalerService.MetricName, actual.MetricName);
        Assert.AreEqual(5, actual.TargetSize);
    }

    [TestMethod]
    public async Task IsActive()
    {
        using CancellationTokenSource tokenSource = new CancellationTokenSource();

        ScalerMetadata metadata = new ScalerMetadata
        {
            AccountName = "unitteststorage",
            Cloud = nameof(CloudEnvironment.AzurePublicCloud),
            MaxActivitiesPerWorker = 5,
            MaxOrchestrationsPerWorker = 3,
            TaskHubName = "UnitTestTaskHub",
            UseManagedIdentity = true,
        };
        AzureStorageAccountInfo accountInfo = metadata.GetAccountInfo(_environment);
        TaskHubQueueUsage usage = new TaskHubQueueUsage(new int[] { 1, 2, 3 }, 10);

        ScaledObjectRef scaledObjectRef = CreateScaledObjectRef(metadata);
        ServerCallContext context = new MockServerCallContext(tokenSource.Token);

        // Argument validation
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _service.IsActive(null!, context)).ConfigureAwait(false);
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _service.IsActive(scaledObjectRef, null!)).ConfigureAwait(false);

        // Set up
        _mockBrowser
            .Setup(s => s
                .GetMonitorAsync(
                    It.Is(accountInfo, AzureStorageAccountInfoEqualityComparer.Instance),
                    "UnitTestTaskHub",
                    tokenSource.Token))
            .Returns(() => ValueTask.FromResult(_mockMonitor.Object));
        _mockMonitor
            .Setup(m => m.GetUsageAsync(tokenSource.Token))
            .Returns(() => ValueTask.FromResult(usage));
        _mockAllocator
            .Setup(a => a.GetWorkerCount(usage.ControlQueueMessages, 3))
            .Returns(2);

        // Run test
        IsActiveResponse response = await _service.IsActive(scaledObjectRef, context).ConfigureAwait(false);
        Assert.IsTrue(response.Result);
    }

    private static GetMetricsRequest CreateGetMetricsRequest(ScalerMetadata metadata)
        => new GetMetricsRequest
        {
            MetricName = DurableTaskAzureStorageScalerService.MetricName,
            ScaledObjectRef = CreateScaledObjectRef(metadata),
        };

    private static ScaledObjectRef CreateScaledObjectRef(ScalerMetadata metadata)
    {
        ScaledObjectRef result = new ScaledObjectRef();

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
