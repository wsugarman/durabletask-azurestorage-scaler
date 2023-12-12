// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Keda.Scaler.DurableTask.AzureStorage.Queues;
using Keda.Scaler.DurableTask.AzureStorage.TaskHub;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHub;

public sealed class TaskHubQueueMonitorTest
{
    private readonly AzureStorageTaskHubInfo _taskHubInfo;
    private readonly QueueServiceClient _queueServiceClient = Substitute.For<QueueServiceClient>();

    private const string TaskHubName = "unit-test";
    private const int PartitionCount = 3;

    public TaskHubQueueMonitorTest()
        => _taskHubInfo = new AzureStorageTaskHubInfo(DateTimeOffset.UtcNow, PartitionCount, TaskHubName);

    [Fact]
    public void GivenNullTaskHubInfo_WhenCreatingTaskHubQueueMonitor_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new TaskHubQueueMonitor(null!, _queueServiceClient, NullLogger.Instance));

    [Fact]
    public void GivenNullQueueServiceClien_WhenCreatingTaskHubQueueMonitor_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new TaskHubQueueMonitor(_taskHubInfo, null!, NullLogger.Instance));

    [Fact]
    public void GivenNullLoggerFactory_WhenCreatingTaskHubQueueMonitor_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new TaskHubQueueMonitor(_taskHubInfo, _queueServiceClient, null!));

    [Fact]
    public void GivenNullLogger_WhenCreatingTaskHubQueueMonitor_ThenThrowArgumentNullException()
    {
        ILoggerFactory loggerFactory = Substitute.For<ILoggerFactory>();
        _ = loggerFactory.CreateLogger(Arg.Any<string>()).Returns((ILogger)null!);

        _ = Assert.Throws<ArgumentNullException>(() => new TaskHubQueueMonitor(_taskHubInfo, _queueServiceClient, NullLogger.Instance));
    }

    [Fact]
    public async Task GivenMissingControlQueue_WhenGettingUsage_ThenReturnNullMonitor()
    {
        using CancellationTokenSource tokenSource = new();
        QueueClient controlQueue0 = Substitute.For<QueueClient>();
        QueueClient controlQueue1 = Substitute.For<QueueClient>();

        _ = _queueServiceClient.GetQueueClient(Arg.Any<string>()).Returns(controlQueue0, controlQueue1);
        _ = controlQueue0
            .GetPropertiesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(GetResponse(5)));
        _ = controlQueue1
            .GetPropertiesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new RequestFailedException((int)HttpStatusCode.NotFound, "Queue not found"));

        TaskHubQueueMonitor monitor = new(_taskHubInfo, _queueServiceClient, NullLogger.Instance);
        TaskHubQueueUsage actual = await monitor.GetUsageAsync(tokenSource.Token);

        _ = _queueServiceClient.Received(1).GetQueueClient(Arg.Is(ControlQueue.GetName(TaskHubName, 0)));
        _ = _queueServiceClient.Received(1).GetQueueClient(Arg.Is(ControlQueue.GetName(TaskHubName, 1)));
        _ = _queueServiceClient.Received(0).GetQueueClient(Arg.Is(ControlQueue.GetName(TaskHubName, 2)));
        _ = await controlQueue0.Received(1).GetPropertiesAsync(Arg.Is(tokenSource.Token));
        _ = await controlQueue1.Received(1).GetPropertiesAsync(Arg.Is(tokenSource.Token));

        Assert.Same(NullTaskHubQueueMonitor.Instance, actual);
    }

    [Fact]
    public async Task GivenMissingWorkItemQueue_WhenGettingUsage_ThenReturnNullMonitor()
    {
        using CancellationTokenSource tokenSource = new();
        QueueClient controlQueue0 = Substitute.For<QueueClient>();
        QueueClient controlQueue1 = Substitute.For<QueueClient>();
        QueueClient controlQueue2 = Substitute.For<QueueClient>();
        QueueClient workItemQueue = Substitute.For<QueueClient>();

        _ = _queueServiceClient.GetQueueClient(Arg.Any<string>()).Returns(controlQueue0, controlQueue1, controlQueue2);
        _ = controlQueue0
            .GetPropertiesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(GetResponse(3)));
        _ = controlQueue1
            .GetPropertiesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(GetResponse(5)));
        _ = controlQueue2
            .GetPropertiesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(GetResponse(4)));
        _ = workItemQueue
            .GetPropertiesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new RequestFailedException((int)HttpStatusCode.NotFound, "Queue not found"));

        TaskHubQueueMonitor monitor = new(_taskHubInfo, _queueServiceClient, NullLogger.Instance);
        TaskHubQueueUsage actual = await monitor.GetUsageAsync(tokenSource.Token);

        _ = _queueServiceClient.Received(1).GetQueueClient(Arg.Is(ControlQueue.GetName(TaskHubName, 0)));
        _ = _queueServiceClient.Received(1).GetQueueClient(Arg.Is(ControlQueue.GetName(TaskHubName, 1)));
        _ = _queueServiceClient.Received(1).GetQueueClient(Arg.Is(ControlQueue.GetName(TaskHubName, 2)));
        _ = _queueServiceClient.Received(1).GetQueueClient(Arg.Is(WorkItemQueue.GetName(TaskHubName)));
        _ = await controlQueue0.Received(1).GetPropertiesAsync(Arg.Is(tokenSource.Token));
        _ = await controlQueue1.Received(1).GetPropertiesAsync(Arg.Is(tokenSource.Token));
        _ = await controlQueue2.Received(1).GetPropertiesAsync(Arg.Is(tokenSource.Token));
        _ = await workItemQueue.Received(1).GetPropertiesAsync(Arg.Is(tokenSource.Token));

        Assert.Same(NullTaskHubQueueMonitor.Instance, actual);
    }

    [Fact]
    public async Task GivenAvailableQueues_WhenGettingUsage_ThenReturnMessageCountSummary()
    {
        using CancellationTokenSource tokenSource = new();
        QueueClient controlQueue0 = Substitute.For<QueueClient>();
        QueueClient controlQueue1 = Substitute.For<QueueClient>();
        QueueClient controlQueue2 = Substitute.For<QueueClient>();
        QueueClient workItemQueue = Substitute.For<QueueClient>();

        _ = _queueServiceClient.GetQueueClient(Arg.Any<string>()).Returns(controlQueue0, controlQueue1, controlQueue2);
        _ = controlQueue0
            .GetPropertiesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(GetResponse(3)));
        _ = controlQueue1
            .GetPropertiesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(GetResponse(5)));
        _ = controlQueue2
            .GetPropertiesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(GetResponse(4)));
        _ = workItemQueue
            .GetPropertiesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(GetResponse(1)));

        TaskHubQueueMonitor monitor = new(_taskHubInfo, _queueServiceClient, NullLogger.Instance);
        TaskHubQueueUsage actual = await monitor.GetUsageAsync(tokenSource.Token);

        _ = _queueServiceClient.Received(1).GetQueueClient(Arg.Is(ControlQueue.GetName(TaskHubName, 0)));
        _ = _queueServiceClient.Received(1).GetQueueClient(Arg.Is(ControlQueue.GetName(TaskHubName, 1)));
        _ = _queueServiceClient.Received(1).GetQueueClient(Arg.Is(ControlQueue.GetName(TaskHubName, 2)));
        _ = _queueServiceClient.Received(1).GetQueueClient(Arg.Is(WorkItemQueue.GetName(TaskHubName)));
        _ = await controlQueue0.Received(1).GetPropertiesAsync(Arg.Is(tokenSource.Token));
        _ = await controlQueue1.Received(1).GetPropertiesAsync(Arg.Is(tokenSource.Token));
        _ = await controlQueue2.Received(1).GetPropertiesAsync(Arg.Is(tokenSource.Token));
        _ = await workItemQueue.Received(1).GetPropertiesAsync(Arg.Is(tokenSource.Token));

        Assert.Equal(PartitionCount, actual.ControlQueueMessages.Count);
        Assert.Equal(3, actual.ControlQueueMessages[0]);
        Assert.Equal(5, actual.ControlQueueMessages[1]);
        Assert.Equal(4, actual.ControlQueueMessages[2]);
        Assert.Equal(1, actual.WorkItemQueueMessages);
    }

    private static Response<QueueProperties> GetResponse(int length)
    {
        QueueProperties properties = Substitute.For<QueueProperties>();
        _ = properties.ApproximateMessagesCount.Returns(length);

        Response response = Substitute.For<Response>();
        _ = response.Status.Returns((int)HttpStatusCode.OK);

        return Response.FromValue(properties, response);
    }
}
