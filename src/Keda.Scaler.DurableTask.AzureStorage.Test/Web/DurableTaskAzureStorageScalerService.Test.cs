// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Keda.Scaler.DurableTask.AzureStorage.TaskHubs;
using Keda.Scaler.DurableTask.AzureStorage.Test.Logging;
using Keda.Scaler.DurableTask.AzureStorage.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Web;

public sealed class DurableTaskAzureStorageScalerServiceTest : IDisposable
{
    private readonly ITaskHub _taskHub = Substitute.For<ITaskHub>();
    private readonly TaskHubOptions _options = new() { TaskHubName = "UnitTest" };
    private readonly ILoggerFactory _loggerFactory;
    private readonly DurableTaskScaleManager _scaleManager;
    private readonly DurableTaskAzureStorageScalerService _service;

    public DurableTaskAzureStorageScalerServiceTest(ITestOutputHelper outputHelper)
    {
        IOptionsSnapshot<TaskHubOptions> _optionsSnapshot = Substitute.For<IOptionsSnapshot<TaskHubOptions>>();
        _ = _optionsSnapshot.Get(default).Returns(_options);
        _loggerFactory = XUnitLogger.CreateFactory(outputHelper);
        _scaleManager = Substitute.For<DurableTaskScaleManager>(_taskHub, _optionsSnapshot, _loggerFactory);
        _service = new(_scaleManager);
    }

    public void Dispose()
        => _loggerFactory.Dispose();

    [Fact]
    public Task GivenNullRequest_WhenGettingMetrics_ThenThrowArgumentNullException()
        => Assert.ThrowsAsync<ArgumentNullException>(() => _service.GetMetrics(null!, new MockServerCallContext()));

    [Fact]
    public Task GivenNullContext_WhenGettingMetrics_ThenThrowArgumentNullException()
        => Assert.ThrowsAsync<ArgumentNullException>(() => _service.GetMetrics(new GetMetricsRequest(), null!));

    [Fact]
    public async Task GivenRequest_WhenGettingMetrics_ThenReturnMetricValues()
    {
        MetricValue expected = new()
        {
            MetricName = DurableTaskScaleManager.MetricName,
            MetricValue_ = 42,
        };

        _ = _scaleManager.GetKedaMetricValueAsync(default).ReturnsForAnyArgs(expected);

        using CancellationTokenSource cts = new();
        GetMetricsResponse actual = await _service.GetMetrics(new GetMetricsRequest(), new MockServerCallContext(cts.Token));

        _ = await _scaleManager.Received(1).GetKedaMetricValueAsync(cts.Token);
        Assert.Same(actual.MetricValues.Single(), expected);
    }

    [Fact]
    public Task GivenNullScaledObjectRef_WhenGettingMetricSpec_ThenThrowArgumentNullException()
        => Assert.ThrowsAsync<ArgumentNullException>(() => _service.GetMetricSpec(null!, new MockServerCallContext()));

    [Fact]
    public Task GivenNullContext_WhenGettingMetricSpec_ThenThrowArgumentNullException()
        => Assert.ThrowsAsync<ArgumentNullException>(() => _service.GetMetricSpec(new ScaledObjectRef(), null!));

    [Fact]
    public async Task GivenRequest_WhenGettingMetricSpec_ThenReturnMetricTarget()
    {
        MetricSpec expected = new()
        {
            MetricName = DurableTaskScaleManager.MetricName,
            TargetSize = 1,
        };

        _ = _scaleManager.KedaMetricSpec.Returns(expected);

        using CancellationTokenSource cts = new();
        GetMetricSpecResponse actual = await _service.GetMetricSpec(new ScaledObjectRef(), new MockServerCallContext(cts.Token));

        _ = _scaleManager.Received(1).KedaMetricSpec;
        Assert.Same(actual.MetricSpecs.Single(), expected);
    }

    [Fact]
    public Task GivenNullRequest_WhenCheckingIfActive_ThenThrowArgumentNullException()
        => Assert.ThrowsAsync<ArgumentNullException>(() => _service.IsActive(null!, new MockServerCallContext()));

    [Fact]
    public Task GivenNullContext_WhenCheckingIfActive_ThenThrowArgumentNullException()
        => Assert.ThrowsAsync<ArgumentNullException>(() => _service.IsActive(new ScaledObjectRef(), null!));

    [Fact]
    public async Task GivenRequest_WhenCheckingIfActive_ThenReturnResponse()
    {
        _ = _scaleManager.IsActiveAsync(default).ReturnsForAnyArgs(true);

        using CancellationTokenSource cts = new();
        IsActiveResponse actual = await _service.IsActive(new ScaledObjectRef(), new MockServerCallContext(cts.Token));

        _ = await _scaleManager.Received(1).IsActiveAsync(cts.Token);
        Assert.True(actual.Result);
    }
}
