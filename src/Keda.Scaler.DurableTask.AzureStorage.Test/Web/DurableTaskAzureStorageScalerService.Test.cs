// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Keda.Scaler.DurableTask.AzureStorage.Accounts;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Keda.Scaler.DurableTask.AzureStorage.TaskHub;
using Keda.Scaler.DurableTask.AzureStorage.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Web;

public sealed class DurableTaskAzureStorageScalerServiceTest : IDisposable
{
    private readonly IStorageAccountClientFactory<BlobServiceClient> _blobServiceClientFactory = Substitute.For<IStorageAccountClientFactory<BlobServiceClient>>();
    private readonly IStorageAccountClientFactory<QueueServiceClient> _queueServiceClientFactory = Substitute.For<IStorageAccountClientFactory<QueueServiceClient>>();
    private readonly AzureStorageTaskHubClient _taskHubClient;
    private readonly IOrchestrationAllocator _allocator = Substitute.For<IOrchestrationAllocator>();
    private readonly ServiceProvider _serviceProvider;
    private readonly DurableTaskAzureStorageScalerService _service;

    public DurableTaskAzureStorageScalerServiceTest(ITestOutputHelper outputHelper)
    {
        _taskHubClient = Substitute.For<AzureStorageTaskHubClient>(
            _blobServiceClientFactory,
            _queueServiceClientFactory,
            NullLoggerFactory.Instance);

        _serviceProvider = new ServiceCollection()
            .AddSingleton(_taskHubClient)
            .AddSingleton(_allocator)
            .AddLogging(x => x.AddXUnit(outputHelper))
            .BuildServiceProvider();

        _service = new DurableTaskAzureStorageScalerService(_serviceProvider);
    }

    public void Dispose()
        => _serviceProvider.Dispose();

    [Fact]
    public void GivenNullServiceProvider_WhenCreatingDurableTaskAzureStorageScalerService_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new DurableTaskAzureStorageScalerService(null!));

    [Fact]
    public void GivenMissingAzureStorageTaskHubClientService_WhenCreatingDurableTaskAzureStorageScalerService_ThenThrowInvalidOperationException()
    {
        using ServiceProvider serviceProvider = new ServiceCollection()
            .AddSingleton(_allocator)
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .BuildServiceProvider();

        _ = Assert.Throws<InvalidOperationException>(() => new DurableTaskAzureStorageScalerService(serviceProvider));
    }

    [Fact]
    public void GivenMissingOrchestrationAllocatorService_WhenCreatingDurableTaskAzureStorageScalerService_ThenThrowInvalidOperationException()
    {
        using ServiceProvider serviceProvider = new ServiceCollection()
            .AddSingleton(_taskHubClient)
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .BuildServiceProvider();

        _ = Assert.Throws<InvalidOperationException>(() => new DurableTaskAzureStorageScalerService(serviceProvider));
    }

    [Fact]
    public void GivenMissingLoggerFactoryService_WhenCreatingDurableTaskAzureStorageScalerService_ThenThrowInvalidOperationException()
    {
        using ServiceProvider serviceProvider = new ServiceCollection()
            .AddSingleton(_taskHubClient)
            .AddSingleton(_allocator)
            .BuildServiceProvider();

        _ = Assert.Throws<InvalidOperationException>(() => new DurableTaskAzureStorageScalerService(serviceProvider));
    }

    [Fact]
    public Task GivenNullRequest_WhenGettingMetrics_ThenThrowArgumentNullException()
        => Assert.ThrowsAsync<ArgumentNullException>(() => _service.GetMetrics(null!, new MockServerCallContext()));

    [Fact]
    public Task GivenNullContext_WhenGettingMetrics_ThenThrowArgumentNullException()
        => Assert.ThrowsAsync<ArgumentNullException>(() => _service.GetMetrics(new GetMetricsRequest(), null!));

    [Fact]
    public Task GivenInvalidMetadata_WhenGettingMetrics_ThenThrowValidationException()
    {
        ScalerMetadata metadata = new()
        {
            AccountName = "unitteststorage",
            Cloud = nameof(CloudEnvironment.AzurePublicCloud),
            Connection = "UseDevelopmentStorage=true",
            TaskHubName = "UnitTestTaskHub",
        };

        ScaledObjectRef scaledObjectRef = CreateScaledObjectRef(metadata);

        GetMetricsRequest request = new() { ScaledObjectRef = scaledObjectRef };
        return Assert.ThrowsAsync<ValidationException>(() => _service.GetMetrics(request, new MockServerCallContext()));
    }

    [Fact]
    public async Task GivenValidMetadata_WhenGettingMetrics_ThenReturnMetricValues()
    {
        const int MaxActivities = 3;
        const int MaxOrchestrations = 2;
        const string TaskHubName = "UnitTestTaskHub";
        const int WorkerCount = 4;

        ScalerMetadata metadata = new()
        {
            AccountName = "unitteststorage",
            Cloud = nameof(CloudEnvironment.AzurePublicCloud),
            MaxActivitiesPerWorker = MaxActivities,
            MaxOrchestrationsPerWorker = MaxOrchestrations,
            TaskHubName = TaskHubName,
        };

        AzureStorageAccountInfo accountInfo = metadata.GetAccountInfo();

        using CancellationTokenSource tokenSource = new();
        GetMetricsRequest request = CreateGetMetricsRequest(metadata);
        MockServerCallContext context = new(tokenSource.Token);

        ITaskHubQueueMonitor monitor = Substitute.For<ITaskHubQueueMonitor>();
        TaskHubQueueUsage usage = new([1, 2, 3, 4], 1);
        _ = _taskHubClient
            .GetMonitorAsync(default!, default!, default)
            .ReturnsForAnyArgs(monitor);
        _ = monitor
            .GetUsageAsync(default)
            .ReturnsForAnyArgs(usage);
        _ = _allocator
            .GetWorkerCount(default!, default)
            .ReturnsForAnyArgs(WorkerCount);

        GetMetricsResponse actual = await _service.GetMetrics(request, context);

        _ = await _taskHubClient
            .Received(1)
            .GetMonitorAsync(
                Arg.Is<AzureStorageAccountInfo>(a => AzureStorageAccountInfoEqualityComparer.Instance.Equals(a, accountInfo)),
                Arg.Is(TaskHubName),
                Arg.Is(tokenSource.Token));
        _ = await monitor
            .Received(1)
            .GetUsageAsync(Arg.Is(tokenSource.Token));
        _ = _allocator
            .Received(1)
            .GetWorkerCount(
                Arg.Is<IReadOnlyList<int>>(t => t.SequenceEqual(new List<int>() { 1, 2, 3, 4 })),
                Arg.Is(MaxOrchestrations));

        int expected = usage.WorkItemQueueMessages + (WorkerCount * MaxActivities);
        Assert.Equal(DurableTaskAzureStorageScalerService.MetricName, actual.MetricValues.Single().MetricName);
        Assert.Equal(expected, actual.MetricValues.Single().MetricValue_);
    }

    [Fact]
    public Task GivenNullScaledObjectRef_WhenGettingMetricSpec_ThenThrowArgumentNullException()
        => Assert.ThrowsAsync<ArgumentNullException>(() => _service.GetMetricSpec(null!, new MockServerCallContext()));

    [Fact]
    public Task GivenNullContext_WhenGettingMetricSpec_ThenThrowArgumentNullException()
        => Assert.ThrowsAsync<ArgumentNullException>(() => _service.GetMetricSpec(new ScaledObjectRef(), null!));

    [Fact]
    public Task GivenInvalidMetadata_WhenGettingMetricSpec_ThenThrowValidationException()
    {
        ScalerMetadata metadata = new()
        {
            AccountName = "unitteststorage",
            Cloud = nameof(CloudEnvironment.AzurePublicCloud),
            Connection = "UseDevelopmentStorage=true",
            TaskHubName = "UnitTestTaskHub",
        };

        ScaledObjectRef scaledObjectRef = CreateScaledObjectRef(metadata);
        return Assert.ThrowsAsync<ValidationException>(() => _service.GetMetricSpec(scaledObjectRef, new MockServerCallContext()));
    }

    [Fact]
    public async Task GivenValidMetadata_WhenGettingMetricSpec_ThenReturnMetricTarget()
    {
        const int MaxActivities = 3;
        const string TaskHubName = "UnitTestTaskHub";

        ScalerMetadata metadata = new()
        {
            AccountName = "unitteststorage",
            Cloud = nameof(CloudEnvironment.AzurePublicCloud),
            MaxActivitiesPerWorker = MaxActivities,
            TaskHubName = TaskHubName,
        };

        using CancellationTokenSource tokenSource = new();
        ScaledObjectRef scaledObjectRef = CreateScaledObjectRef(metadata);
        MockServerCallContext context = new(tokenSource.Token);

        GetMetricSpecResponse actual = await _service.GetMetricSpec(scaledObjectRef, context);

        Assert.Equal(DurableTaskAzureStorageScalerService.MetricName, actual.MetricSpecs.Single().MetricName);
        Assert.Equal(MaxActivities, actual.MetricSpecs.Single().TargetSize);
    }

    [Fact]
    public Task GivenNullRequest_WhenCheckingIfActive_ThenThrowArgumentNullException()
        => Assert.ThrowsAsync<ArgumentNullException>(() => _service.IsActive(null!, new MockServerCallContext()));

    [Fact]
    public Task GivenNullContext_WhenCheckingIfActive_ThenThrowArgumentNullException()
        => Assert.ThrowsAsync<ArgumentNullException>(() => _service.IsActive(new ScaledObjectRef(), null!));

    [Fact]
    public Task GivenInvalidMetadata_WhenCheckingIfActive_ThenThrowValidationException()
    {
        ScalerMetadata metadata = new()
        {
            AccountName = "unitteststorage",
            Cloud = nameof(CloudEnvironment.AzurePublicCloud),
            Connection = "UseDevelopmentStorage=true",
            TaskHubName = "UnitTestTaskHub",
        };

        ScaledObjectRef scaledObjectRef = CreateScaledObjectRef(metadata);
        return Assert.ThrowsAsync<ValidationException>(() => _service.IsActive(scaledObjectRef, new MockServerCallContext()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GivenValidMetadata_WhenCheckingIfActive_ThenReturnMetricValues(bool hasActivity)
    {
        const string TaskHubName = "UnitTestTaskHub";

        ScalerMetadata metadata = new()
        {
            AccountName = "unitteststorage",
            Cloud = nameof(CloudEnvironment.AzurePublicCloud),
            TaskHubName = TaskHubName,
        };

        AzureStorageAccountInfo accountInfo = metadata.GetAccountInfo();

        using CancellationTokenSource tokenSource = new();
        ScaledObjectRef scaledObjectRef = CreateScaledObjectRef(metadata);
        MockServerCallContext context = new(tokenSource.Token);

        ITaskHubQueueMonitor monitor = Substitute.For<ITaskHubQueueMonitor>();
        TaskHubQueueUsage usage = hasActivity ? new([1, 2, 3, 4], 1) : new([0, 0, 0, 0], 0);
        _ = _taskHubClient
            .GetMonitorAsync(default!, default!, default)
            .ReturnsForAnyArgs(monitor);
        _ = monitor
            .GetUsageAsync(default)
            .ReturnsForAnyArgs(usage);

        IsActiveResponse actual = await _service.IsActive(scaledObjectRef, context);

        _ = await _taskHubClient
            .Received(1)
            .GetMonitorAsync(
                Arg.Is<AzureStorageAccountInfo>(a => AzureStorageAccountInfoEqualityComparer.Instance.Equals(a, accountInfo)),
                Arg.Is(TaskHubName),
                Arg.Is(tokenSource.Token));
        _ = await monitor
            .Received(1)
            .GetUsageAsync(Arg.Is(tokenSource.Token));

        Assert.Equal(hasActivity, actual.Result);
    }

    private static GetMetricsRequest CreateGetMetricsRequest(ScalerMetadata metadata)
    {
        return new()
        {
            MetricName = DurableTaskAzureStorageScalerService.MetricName,
            ScaledObjectRef = CreateScaledObjectRef(metadata),
        };
    }

    private static ScaledObjectRef CreateScaledObjectRef(ScalerMetadata metadata)
    {
        ScaledObjectRef result = new()
        {
            ScalerMetadata =
            {
                { nameof(ScalerMetadata.MaxActivitiesPerWorker), metadata.MaxActivitiesPerWorker.ToString(CultureInfo.InvariantCulture) },
                { nameof(ScalerMetadata.MaxOrchestrationsPerWorker), metadata.MaxOrchestrationsPerWorker.ToString(CultureInfo.InvariantCulture) },
                { nameof(ScalerMetadata.UseManagedIdentity), metadata.UseManagedIdentity.ToString(CultureInfo.InvariantCulture) },
            }
        };

        if (metadata.AccountName is not null)
            result.ScalerMetadata[nameof(ScalerMetadata.AccountName)] = metadata.AccountName;

        if (metadata.Connection is not null)
            result.ScalerMetadata[nameof(ScalerMetadata.Connection)] = metadata.Connection;

        if (metadata.ConnectionFromEnv is not null)
            result.ScalerMetadata[nameof(ScalerMetadata.ConnectionFromEnv)] = metadata.ConnectionFromEnv;

        if (metadata.TaskHubName is not null)
            result.ScalerMetadata[nameof(ScalerMetadata.TaskHubName)] = metadata.TaskHubName;

        if (metadata.Cloud is not null)
            result.ScalerMetadata[nameof(ScalerMetadata.Cloud)] = metadata.Cloud;

        return result;
    }
}
