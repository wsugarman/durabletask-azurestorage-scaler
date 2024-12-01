// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Keda.Scaler.DurableTask.AzureStorage.TaskHub;
using Keda.Scaler.DurableTask.AzureStorage.Test.Logging;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using Xunit.Abstractions;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHub;

public sealed class TaskHubQueueMonitorTest : IDisposable
{
    private readonly AzureStorageTaskHubInfo _taskHubInfo;
    private readonly QueueServiceClient _queueServiceClient = Substitute.For<QueueServiceClient>();
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;

    private const string TaskHubName = "unit-test";
    private const int PartitionCount = 3;

    private static readonly Func<int, QueueProperties> QueuePropertiesFactory = CreateQueuePropertiesFactory();

    public TaskHubQueueMonitorTest(ITestOutputHelper outputHelper)
    {
        _taskHubInfo = new AzureStorageTaskHubInfo(DateTimeOffset.UtcNow, PartitionCount, TaskHubName);
        _loggerFactory = XUnitLogger.CreateFactory(outputHelper);
        _logger = _loggerFactory.CreateLogger(LogCategories.Default);
    }

    public void Dispose()
        => _loggerFactory.Dispose();

    [Fact]
    public void GivenNullTaskHubInfo_WhenCreatingTaskHubQueueMonitor_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new TaskHubQueueMonitor(null!, _queueServiceClient, _logger));

    [Fact]
    public void GivenNullQueueServiceClien_WhenCreatingTaskHubQueueMonitor_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new TaskHubQueueMonitor(_taskHubInfo, null!, _logger));

    [Fact]
    public void GivenNullLogger_WhenCreatingTaskHubQueueMonitor_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new TaskHubQueueMonitor(_taskHubInfo, _queueServiceClient, null!));

    [Fact]
    public async Task GivenMissingControlQueue_WhenGettingUsage_ThenReturnNullMonitor()
    {
        using CancellationTokenSource tokenSource = new();
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

        AzureStorage.TaskHub.TaskHub monitor = new(_taskHubInfo, _queueServiceClient, _logger);
        TaskHubQueueUsage actual = await monitor.GetUsageAsync(tokenSource.Token);

        _ = _queueServiceClient.Received(1).GetQueueClient(Arg.Is(ControlQueue.GetName(TaskHubName, 0)));
        _ = _queueServiceClient.Received(1).GetQueueClient(Arg.Is(ControlQueue.GetName(TaskHubName, 1)));
        _ = _queueServiceClient.Received(0).GetQueueClient(Arg.Is(ControlQueue.GetName(TaskHubName, 2)));
        _ = await controlQueue0.Received(1).GetPropertiesAsync(Arg.Is(tokenSource.Token));
        _ = await controlQueue1.Received(1).GetPropertiesAsync(Arg.Is(tokenSource.Token));

        Assert.Same(TaskHubQueueUsage.None, actual);
    }

    [Fact]
    public async Task GivenMissingWorkItemQueue_WhenGettingUsage_ThenReturnNullMonitor()
    {
        using CancellationTokenSource tokenSource = new();
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

        AzureStorage.TaskHub.TaskHub monitor = new(_taskHubInfo, _queueServiceClient, _logger);
        TaskHubQueueUsage actual = await monitor.GetUsageAsync(tokenSource.Token);

        _ = _queueServiceClient.Received(1).GetQueueClient(Arg.Is(ControlQueue.GetName(TaskHubName, 0)));
        _ = _queueServiceClient.Received(1).GetQueueClient(Arg.Is(ControlQueue.GetName(TaskHubName, 1)));
        _ = _queueServiceClient.Received(1).GetQueueClient(Arg.Is(ControlQueue.GetName(TaskHubName, 2)));
        _ = _queueServiceClient.Received(1).GetQueueClient(Arg.Is(WorkItemQueue.GetName(TaskHubName)));
        _ = await controlQueue0.Received(1).GetPropertiesAsync(Arg.Is(tokenSource.Token));
        _ = await controlQueue1.Received(1).GetPropertiesAsync(Arg.Is(tokenSource.Token));
        _ = await controlQueue2.Received(1).GetPropertiesAsync(Arg.Is(tokenSource.Token));
        _ = await workItemQueue.Received(1).GetPropertiesAsync(Arg.Is(tokenSource.Token));

        Assert.Same(TaskHubQueueUsage.None, actual);
    }

    [Fact]
    public async Task GivenAvailableQueues_WhenGettingUsage_ThenReturnMessageCountSummary()
    {
        using CancellationTokenSource tokenSource = new();
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

        AzureStorage.TaskHub.TaskHub monitor = new(_taskHubInfo, _queueServiceClient, _logger);
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
