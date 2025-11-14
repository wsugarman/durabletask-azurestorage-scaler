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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHubs;

[TestClass]
public sealed class TaskHubTest
{
    private readonly ITaskHubPartitionManager _partitionManager = Substitute.For<ITaskHubPartitionManager>();
    private readonly QueueServiceClient _queueServiceClient = Substitute.For<QueueServiceClient>();
    private readonly IOptionsSnapshot<TaskHubOptions> _optionsSnapshot = Substitute.For<IOptionsSnapshot<TaskHubOptions>>();
    private readonly TaskHub _taskHub;

    private const string TaskHubName = "UnitTest";
    private const int PartitionCount = 3;

    private static readonly Func<int, QueueProperties> QueuePropertiesFactory = CreateQueuePropertiesFactory();

    public TaskHubTest()
    {
        List<string> partitionIds = [.. Enumerable.Repeat(TaskHubName, PartitionCount).Select(ControlQueue.GetName)];

        _ = _partitionManager.GetPartitionsAsync(default).ReturnsForAnyArgs(partitionIds);
        _ = _optionsSnapshot.Get(default).Returns(new TaskHubOptions { TaskHubName = TaskHubName });
        _taskHub = new(_partitionManager, _queueServiceClient, _optionsSnapshot, NullLoggerFactory.Instance);
    }

    public required TestContext TestContext { get; init; }

    [TestMethod]
    public void GivenNullTaskHubInfo_WhenCreatingTaskHub_ThenThrowArgumentNullException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => new TaskHub(null!, _queueServiceClient, _optionsSnapshot, NullLoggerFactory.Instance));

    [TestMethod]
    public void GivenNullQueueServiceClient_WhenCreatingTaskHub_ThenThrowArgumentNullException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => new TaskHub(_partitionManager, null!, _optionsSnapshot, NullLoggerFactory.Instance));

    [TestMethod]
    public void GivenNullOptionsSnapshot_WhenCreatingTablePartitionManager_ThenThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new TaskHub(_partitionManager, _queueServiceClient, null!, NullLoggerFactory.Instance));

        IOptionsSnapshot<TaskHubOptions> nullSnapshot = Substitute.For<IOptionsSnapshot<TaskHubOptions>>();
        _ = nullSnapshot.Get(default).Returns(default(TaskHubOptions));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new TaskHub(_partitionManager, _queueServiceClient, nullSnapshot, NullLoggerFactory.Instance));
    }

    [TestMethod]
    public void GivenNullLoggerFactory_WhenCreatingTablePartitionManager_ThenThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new TaskHub(_partitionManager, _queueServiceClient, _optionsSnapshot, null!));

        ILoggerFactory nullFactory = Substitute.For<ILoggerFactory>();
        _ = nullFactory.CreateLogger(default!).ReturnsForAnyArgs(default(ILogger));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new TaskHub(_partitionManager, _queueServiceClient, _optionsSnapshot, nullFactory));
    }

    [TestMethod]
    public async ValueTask GivenNoPartitions_WhenGettingUsage_ThenReturnNoUsage()
    {
        _ = _partitionManager.GetPartitionsAsync(TestContext.CancellationToken).ReturnsForAnyArgs([]);

        using CancellationTokenSource cts = new();
        TaskHubQueueUsage actual = await _taskHub.GetUsageAsync(cts.Token);

        _ = await _partitionManager.Received(1).GetPartitionsAsync(cts.Token);
        _ = _queueServiceClient.DidNotReceiveWithAnyArgs().GetQueueClient(default);

        Assert.AreSame(TaskHubQueueUsage.None, actual);
    }

    [TestMethod]
    public async ValueTask GivenMissingControlQueue_WhenGettingUsage_ThenReturnNoUsage()
    {
        QueueClient controlQueue0 = Substitute.For<QueueClient>();
        QueueClient controlQueue1 = Substitute.For<QueueClient>();

        _ = _queueServiceClient
            .GetQueueClient(default!)
            .ReturnsForAnyArgs(controlQueue0, controlQueue1);
        _ = controlQueue0
            .GetPropertiesAsync(TestContext.CancellationToken)
            .ReturnsForAnyArgs(x => Task.FromResult(GetResponse(5)));
        _ = controlQueue1
            .GetPropertiesAsync(TestContext.CancellationToken)
            .ThrowsAsyncForAnyArgs(new RequestFailedException((int)HttpStatusCode.NotFound, "Queue not found"));

        using CancellationTokenSource cts = new();
        TaskHubQueueUsage actual = await _taskHub.GetUsageAsync(cts.Token);

        _ = await _partitionManager.Received(1).GetPartitionsAsync(cts.Token);
        _ = _queueServiceClient.Received(1).GetQueueClient(ControlQueue.GetName(TaskHubName, 0));
        _ = _queueServiceClient.Received(1).GetQueueClient(ControlQueue.GetName(TaskHubName, 1));
        _ = _queueServiceClient.Received(0).GetQueueClient(ControlQueue.GetName(TaskHubName, 2));
        _ = await controlQueue0.Received(1).GetPropertiesAsync(cts.Token);
        _ = await controlQueue1.Received(1).GetPropertiesAsync(cts.Token);

        Assert.AreSame(TaskHubQueueUsage.None, actual);
    }

    [TestMethod]
    public async ValueTask GivenMissingWorkItemQueue_WhenGettingUsage_ThenReturnNoUsage()
    {
        QueueClient controlQueue0 = Substitute.For<QueueClient>();
        QueueClient controlQueue1 = Substitute.For<QueueClient>();
        QueueClient controlQueue2 = Substitute.For<QueueClient>();
        QueueClient workItemQueue = Substitute.For<QueueClient>();

        _ = _queueServiceClient
            .GetQueueClient(default)
            .ReturnsForAnyArgs(controlQueue0, controlQueue1, controlQueue2, workItemQueue);
        _ = controlQueue0
            .GetPropertiesAsync(TestContext.CancellationToken)
            .ReturnsForAnyArgs(x => Task.FromResult(GetResponse(3)));
        _ = controlQueue1
            .GetPropertiesAsync(TestContext.CancellationToken)
            .ReturnsForAnyArgs(x => Task.FromResult(GetResponse(5)));
        _ = controlQueue2
            .GetPropertiesAsync(TestContext.CancellationToken)
            .ReturnsForAnyArgs(x => Task.FromResult(GetResponse(4)));
        _ = workItemQueue
            .GetPropertiesAsync(TestContext.CancellationToken)
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

        Assert.AreSame(TaskHubQueueUsage.None, actual);
    }

    [TestMethod]
    public async ValueTask GivenAvailableQueues_WhenGettingUsage_ThenReturnMessageCountSummary()
    {
        QueueClient controlQueue0 = Substitute.For<QueueClient>();
        QueueClient controlQueue1 = Substitute.For<QueueClient>();
        QueueClient controlQueue2 = Substitute.For<QueueClient>();
        QueueClient workItemQueue = Substitute.For<QueueClient>();

        _ = _queueServiceClient
            .GetQueueClient(default)
            .ReturnsForAnyArgs(controlQueue0, controlQueue1, controlQueue2, workItemQueue);
        _ = controlQueue0
            .GetPropertiesAsync(TestContext.CancellationToken)
            .ReturnsForAnyArgs(x => Task.FromResult(GetResponse(3)));
        _ = controlQueue1
            .GetPropertiesAsync(TestContext.CancellationToken)
            .ReturnsForAnyArgs(x => Task.FromResult(GetResponse(5)));
        _ = controlQueue2
            .GetPropertiesAsync(TestContext.CancellationToken)
            .ReturnsForAnyArgs(x => Task.FromResult(GetResponse(4)));
        _ = workItemQueue
            .GetPropertiesAsync(TestContext.CancellationToken)
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

        Assert.HasCount(PartitionCount, actual.ControlQueueMessages);
        Assert.AreEqual(3, actual.ControlQueueMessages[0]);
        Assert.AreEqual(5, actual.ControlQueueMessages[1]);
        Assert.AreEqual(4, actual.ControlQueueMessages[2]);
        Assert.AreEqual(1, actual.WorkItemQueueMessages);
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
