// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Keda.Scaler.DurableTask.AzureStorage.TaskHubs;
using Keda.Scaler.DurableTask.AzureStorage.Test.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using Xunit.Abstractions;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHubs;

public sealed class TaskHubTest : IDisposable
{
    private readonly ITaskHubPartitionManager _partitionManager = Substitute.For<ITaskHubPartitionManager>();
    private readonly QueueServiceClient _queueServiceClient = Substitute.For<QueueServiceClient>();
    private readonly IOptionsSnapshot<TaskHubOptions> _optionsSnapshot = Substitute.For<IOptionsSnapshot<TaskHubOptions>>();
    private readonly ILoggerFactory _loggerFactory;
    private readonly TaskHub _taskHub;

    private const string TaskHubName = "UnitTest";
    private const int PartitionCount = 3;

    private static readonly Func<int, QueueProperties> QueuePropertiesFactory = CreateQueuePropertiesFactory();

    public TaskHubTest(ITestOutputHelper outputHelper)
    {
        List<string> partitionIds = Enumerable
            .Repeat(TaskHubName, PartitionCount)
            .Select(ControlQueue.GetName)
            .ToList();

        _ = _partitionManager.GetPartitionsAsync(default).ReturnsForAnyArgs(partitionIds);
        _ = _optionsSnapshot.Get(default).Returns(new TaskHubOptions { TaskHubName = TaskHubName });
        _loggerFactory = XUnitLogger.CreateFactory(outputHelper);
        _taskHub = new(_partitionManager, _queueServiceClient, _optionsSnapshot, _loggerFactory);
    }

    public void Dispose()
        => _loggerFactory.Dispose();

    [Fact]
    public void GivenNullTaskHubInfo_WhenCreatingTaskHub_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new TaskHub(null!, _queueServiceClient, _optionsSnapshot, _loggerFactory));

    [Fact]
    public void GivenNullQueueServiceClient_WhenCreatingTaskHub_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new TaskHub(_partitionManager, null!, _optionsSnapshot, _loggerFactory));

    [Fact]
    public void GivenNullOptionsSnapshot_WhenCreatingTablePartitionManager_ThenThrowArgumentNullException()
    {
        _ = Assert.Throws<ArgumentNullException>(() => new TaskHub(_partitionManager, _queueServiceClient, null!, _loggerFactory));

        IOptionsSnapshot<TaskHubOptions> nullSnapshot = Substitute.For<IOptionsSnapshot<TaskHubOptions>>();
        _ = nullSnapshot.Get(default).Returns(default(TaskHubOptions));
        _ = Assert.Throws<ArgumentNullException>(() => new TaskHub(_partitionManager, _queueServiceClient, nullSnapshot, _loggerFactory));
    }

    [Fact]
    public void GivenNullLoggerFactory_WhenCreatingTablePartitionManager_ThenThrowArgumentNullException()
    {
        _ = Assert.Throws<ArgumentNullException>(() => new TaskHub(_partitionManager, _queueServiceClient, _optionsSnapshot, null!));

        ILoggerFactory nullFactory = Substitute.For<ILoggerFactory>();
        _ = nullFactory.CreateLogger(default!).ReturnsForAnyArgs(default(ILogger));
        _ = Assert.Throws<ArgumentNullException>(() => new TaskHub(_partitionManager, _queueServiceClient, _optionsSnapshot, nullFactory));
    }

    [Fact]
    public async Task GivenMissingControlQueue_WhenGettingUsage_ThenReturnNullMonitor()
    {
        QueueClient controlQueue0 = Substitute.For<QueueClient>();
        QueueClient controlQueue1 = Substitute.For<QueueClient>();

        _ = _queueServiceClient
            .GetQueueClient(default!)
            .ReturnsForAnyArgs(controlQueue0, controlQueue1);
        _ = controlQueue0
            .GetPropertiesAsync(default)
            .ReturnsForAnyArgs(x => Task.FromResult(GetResponse(5)));
        _ = controlQueue1
            .GetPropertiesAsync(default)
            .ThrowsAsyncForAnyArgs(new RequestFailedException((int)HttpStatusCode.NotFound, "Queue not found"));

        using CancellationTokenSource cts = new();
        TaskHubQueueUsage actual = await _taskHub.GetUsageAsync(cts.Token);

        _ = await _partitionManager.Received(1).GetPartitionsAsync(cts.Token);
        _ = _queueServiceClient.Received(1).GetQueueClient(ControlQueue.GetName(TaskHubName, 0));
        _ = _queueServiceClient.Received(1).GetQueueClient(ControlQueue.GetName(TaskHubName, 1));
        _ = _queueServiceClient.Received(0).GetQueueClient(ControlQueue.GetName(TaskHubName, 2));
        _ = await controlQueue0.Received(1).GetPropertiesAsync(cts.Token);
        _ = await controlQueue1.Received(1).GetPropertiesAsync(cts.Token);

        Assert.Same(TaskHubQueueUsage.None, actual);
    }

    [Fact]
    public async Task GivenMissingWorkItemQueue_WhenGettingUsage_ThenReturnNullMonitor()
    {
        QueueClient controlQueue0 = Substitute.For<QueueClient>();
        QueueClient controlQueue1 = Substitute.For<QueueClient>();
        QueueClient controlQueue2 = Substitute.For<QueueClient>();
        QueueClient workItemQueue = Substitute.For<QueueClient>();

        _ = _queueServiceClient
            .GetQueueClient(default)
            .ReturnsForAnyArgs(controlQueue0, controlQueue1, controlQueue2, workItemQueue);
        _ = controlQueue0
            .GetPropertiesAsync(default)
            .ReturnsForAnyArgs(x => Task.FromResult(GetResponse(3)));
        _ = controlQueue1
            .GetPropertiesAsync(default)
            .ReturnsForAnyArgs(x => Task.FromResult(GetResponse(5)));
        _ = controlQueue2
            .GetPropertiesAsync(default)
            .ReturnsForAnyArgs(x => Task.FromResult(GetResponse(4)));
        _ = workItemQueue
            .GetPropertiesAsync(default)
            .ThrowsAsyncForAnyArgs(new RequestFailedException((int)HttpStatusCode.NotFound, "Queue not found"));

        using CancellationTokenSource cts = new();
        TaskHubQueueUsage actual = await _taskHub.GetUsageAsync(cts.Token);

        _ = await _partitionManager.Received(1).GetPartitionsAsync(cts.Token);
        _ = _queueServiceClient.Received(1).GetQueueClient(ControlQueue.GetName(TaskHubName, 0));
        _ = _queueServiceClient.Received(1).GetQueueClient(ControlQueue.GetName(TaskHubName, 1));
        _ = _queueServiceClient.Received(1).GetQueueClient(ControlQueue.GetName(TaskHubName, 2));
        _ = _queueServiceClient.Received(1).GetQueueClient(WorkItemQueue.GetName(TaskHubName));
        _ = await controlQueue0.Received(1).GetPropertiesAsync(cts.Token);
        _ = await controlQueue1.Received(1).GetPropertiesAsync(cts.Token);
        _ = await controlQueue2.Received(1).GetPropertiesAsync(cts.Token);
        _ = await workItemQueue.Received(1).GetPropertiesAsync(cts.Token);

        Assert.Same(TaskHubQueueUsage.None, actual);
    }

    [Fact]
    public async Task GivenAvailableQueues_WhenGettingUsage_ThenReturnMessageCountSummary()
    {
        QueueClient controlQueue0 = Substitute.For<QueueClient>();
        QueueClient controlQueue1 = Substitute.For<QueueClient>();
        QueueClient controlQueue2 = Substitute.For<QueueClient>();
        QueueClient workItemQueue = Substitute.For<QueueClient>();

        _ = _queueServiceClient
            .GetQueueClient(default)
            .ReturnsForAnyArgs(controlQueue0, controlQueue1, controlQueue2, workItemQueue);
        _ = controlQueue0
            .GetPropertiesAsync(default)
            .ReturnsForAnyArgs(x => Task.FromResult(GetResponse(3)));
        _ = controlQueue1
            .GetPropertiesAsync(default)
            .ReturnsForAnyArgs(x => Task.FromResult(GetResponse(5)));
        _ = controlQueue2
            .GetPropertiesAsync(default)
            .ReturnsForAnyArgs(x => Task.FromResult(GetResponse(4)));
        _ = workItemQueue
            .GetPropertiesAsync(default)
            .ReturnsForAnyArgs(x => Task.FromResult(GetResponse(1)));

        using CancellationTokenSource cts = new();
        TaskHubQueueUsage actual = await _taskHub.GetUsageAsync(cts.Token);

        _ = await _partitionManager.Received(1).GetPartitionsAsync(cts.Token);
        _ = _queueServiceClient.Received(1).GetQueueClient(ControlQueue.GetName(TaskHubName, 0));
        _ = _queueServiceClient.Received(1).GetQueueClient(ControlQueue.GetName(TaskHubName, 1));
        _ = _queueServiceClient.Received(1).GetQueueClient(ControlQueue.GetName(TaskHubName, 2));
        _ = _queueServiceClient.Received(1).GetQueueClient(WorkItemQueue.GetName(TaskHubName));
        _ = await controlQueue0.Received(1).GetPropertiesAsync(cts.Token);
        _ = await controlQueue1.Received(1).GetPropertiesAsync(cts.Token);
        _ = await controlQueue2.Received(1).GetPropertiesAsync(cts.Token);
        _ = await workItemQueue.Received(1).GetPropertiesAsync(cts.Token);

        Assert.Equal(PartitionCount, actual.ControlQueueMessages.Count);
        Assert.Equal(3, actual.ControlQueueMessages[0]);
        Assert.Equal(5, actual.ControlQueueMessages[1]);
        Assert.Equal(4, actual.ControlQueueMessages[2]);
        Assert.Equal(1, actual.WorkItemQueueMessages);
    }

    private static Response<QueueProperties> GetResponse(int count)
    {
        Response response = Substitute.For<Response>();
        _ = response.Status.Returns((int)HttpStatusCode.OK);

        return Response.FromValue(QueuePropertiesFactory(count), response);
    }

    private static Func<int, QueueProperties> CreateQueuePropertiesFactory()
    {
        ParameterExpression param = Expression.Parameter(typeof(int), "count");
        ParameterExpression variable = Expression.Variable(typeof(QueueProperties), "properties");
        MethodInfo? countSetter = typeof(QueueProperties)
            .GetProperty(nameof(QueueProperties.ApproximateMessagesCount))?
            .GetSetMethod(nonPublic: true);

        return (Func<int, QueueProperties>)Expression
            .Lambda(
                Expression.Block(
                    typeof(QueueProperties),
                    [variable],
                    Expression.Assign(variable, Expression.New(typeof(QueueProperties))),
                    Expression.Call(variable, countSetter!, param),
                    variable),
                param)
            .Compile();
    }
}
