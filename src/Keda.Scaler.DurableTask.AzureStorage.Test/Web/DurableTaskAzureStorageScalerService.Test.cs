// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Keda.Scaler.DurableTask.AzureStorage.TaskHubs;
using Keda.Scaler.DurableTask.AzureStorage.Web;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Web;

[TestClass]
public sealed class DurableTaskAzureStorageScalerServiceTest
{
    private readonly TestContext _testContext;
    private readonly ITaskHub _taskHub;
    private readonly TaskHubOptions _options;
    private readonly DurableTaskScaleManager _scaleManager;
    private readonly DurableTaskAzureStorageScalerService _service;

    public DurableTaskAzureStorageScalerServiceTest(TestContext testContext)
    {
        ArgumentNullException.ThrowIfNull(testContext);

        _testContext = testContext;
        _taskHub = Substitute.For<ITaskHub>();
        _options = new() { TaskHubName = "UnitTest" };

        IOptionsSnapshot<TaskHubOptions> _optionsSnapshot = Substitute.For<IOptionsSnapshot<TaskHubOptions>>();
        _ = _optionsSnapshot.Get(default).Returns(_options);
        _scaleManager = Substitute.For<DurableTaskScaleManager>(_taskHub, _optionsSnapshot, NullLoggerFactory.Instance);
        _service = new(_scaleManager);
    }

    [TestMethod]
    public void GivenNullScalerManager_WhenCreatingService_ThenThrowArgumentNullException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => new DurableTaskAzureStorageScalerService(null!));

    [TestMethod]
    public Task GivenNullRequest_WhenGettingMetrics_ThenThrowArgumentNullException()
        => Assert.ThrowsExactlyAsync<ArgumentNullException>(() => _service.GetMetrics(null!, new MockServerCallContext()));

    [TestMethod]
    public Task GivenNullContext_WhenGettingMetrics_ThenThrowArgumentNullException()
        => Assert.ThrowsExactlyAsync<ArgumentNullException>(() => _service.GetMetrics(new GetMetricsRequest(), null!));

    [TestMethod]
    public async ValueTask GivenRequest_WhenGettingMetrics_ThenReturnMetricValues()
    {
        MetricValue expected = new()
        {
            MetricName = DurableTaskScaleManager.MetricName,
            MetricValue_ = 42,
        };

        _ = _scaleManager.GetKedaMetricValueAsync(_testContext.CancellationToken).ReturnsForAnyArgs(expected);

        using CancellationTokenSource cts = new();
        GetMetricsResponse actual = await _service.GetMetrics(new GetMetricsRequest(), new MockServerCallContext(cts.Token));

        _ = await _scaleManager.Received(1).GetKedaMetricValueAsync(cts.Token);
        Assert.AreSame(expected, Assert.ContainsSingle(actual.MetricValues));
    }

    [TestMethod]
    public Task GivenNullScaledObjectRef_WhenGettingMetricSpec_ThenThrowArgumentNullException()
        => Assert.ThrowsExactlyAsync<ArgumentNullException>(() => _service.GetMetricSpec(null!, new MockServerCallContext()));

    [TestMethod]
    public Task GivenNullContext_WhenGettingMetricSpec_ThenThrowArgumentNullException()
        => Assert.ThrowsExactlyAsync<ArgumentNullException>(() => _service.GetMetricSpec(new ScaledObjectRef(), null!));

    [TestMethod]
    public async ValueTask GivenRequest_WhenGettingMetricSpec_ThenReturnMetricTarget()
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
        Assert.AreSame(expected, Assert.ContainsSingle(actual.MetricSpecs));
    }

    [TestMethod]
    public Task GivenNullRequest_WhenCheckingIfActive_ThenThrowArgumentNullException()
        => Assert.ThrowsExactlyAsync<ArgumentNullException>(() => _service.IsActive(null!, new MockServerCallContext()));

    [TestMethod]
    public Task GivenNullContext_WhenCheckingIfActive_ThenThrowArgumentNullException()
        => Assert.ThrowsExactlyAsync<ArgumentNullException>(() => _service.IsActive(new ScaledObjectRef(), null!));

    [TestMethod]
    public async ValueTask GivenRequest_WhenCheckingIfActive_ThenReturnResponse()
    {
        _ = _scaleManager.IsActiveAsync(_testContext.CancellationToken).ReturnsForAnyArgs(true);

        using CancellationTokenSource cts = new();
        IsActiveResponse actual = await _service.IsActive(new ScaledObjectRef(), new MockServerCallContext(cts.Token));

        _ = await _scaleManager.Received(1).IsActiveAsync(cts.Token);
        Assert.IsTrue(actual.Result);
    }
}
