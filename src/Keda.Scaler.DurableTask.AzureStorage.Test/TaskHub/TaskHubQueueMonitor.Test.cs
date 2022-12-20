// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Keda.Scaler.DurableTask.AzureStorage.Queues;
using Keda.Scaler.DurableTask.AzureStorage.TaskHub;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHub;

[TestClass]
public sealed class TaskHubQueueMonitorTest
{
    private readonly AzureStorageTaskHubInfo _taskHubInfo;
    private readonly Mock<QueueServiceClient> _mockQueueServiceClient;
    private readonly TaskHubQueueMonitor _monitor;

    private static readonly Action<QueueProperties, int> SetApproximateMessagesCount = CreateSetter();

    public TaskHubQueueMonitorTest()
    {
        _taskHubInfo = new AzureStorageTaskHubInfo { PartitionCount = 5, TaskHubName = "unit-test" };
        _mockQueueServiceClient = new Mock<QueueServiceClient>(MockBehavior.Strict);
        _monitor = new TaskHubQueueMonitor(_taskHubInfo, _mockQueueServiceClient.Object, NullLogger.Instance);
    }

    [TestMethod]
    public async Task GetUsageAsync()
    {
        using CancellationTokenSource tokenSource = new CancellationTokenSource();

        // Set up
        Mock<QueueClient>[] mockControlQueueClients = new Mock<QueueClient>[_taskHubInfo.PartitionCount];
        for (int i = 0; i < mockControlQueueClients.Length; i++)
        {
            string name = ControlQueue.GetName(_taskHubInfo.TaskHubName, i);
            QueueProperties properties = new QueueProperties();
            SetApproximateMessagesCount(properties, i);

            mockControlQueueClients[i] = new Mock<QueueClient>(MockBehavior.Strict);
            mockControlQueueClients[i]
                .Setup(c => c.GetPropertiesAsync(tokenSource.Token))
                .Returns(Task.FromResult(Response.FromValue(properties, null!)));
            _mockQueueServiceClient
                .Setup(c => c.GetQueueClient(name))
                .Returns(mockControlQueueClients[i].Object);
        }

        QueueProperties workItemProperties = new QueueProperties();
        SetApproximateMessagesCount(workItemProperties, 30);

        Mock<QueueClient> mockWorkItemsQueueClient = new Mock<QueueClient>(MockBehavior.Strict);
        mockWorkItemsQueueClient
            .Setup(c => c.GetPropertiesAsync(tokenSource.Token))
            .Returns(Task.FromResult(Response.FromValue(workItemProperties, null!)));
        _mockQueueServiceClient
            .Setup(c => c.GetQueueClient(WorkItemQueue.GetName(_taskHubInfo.TaskHubName)))
            .Returns(mockWorkItemsQueueClient.Object);

        // Test successful measurement
        TaskHubQueueUsage actual = await _monitor.GetUsageAsync(tokenSource.Token).ConfigureAwait(false);
        Assert.IsTrue(actual.HasActivity);
        Assert.IsTrue(actual.ControlQueueMessages.Select((x, i) => (Count: x, Index: i)).All(p => p.Count == p.Index));
        Assert.AreEqual(30, actual.WorkItemQueueMessages);

        // Test missing work item queue
        mockWorkItemsQueueClient.Reset();
        mockWorkItemsQueueClient
            .Setup(c => c.GetPropertiesAsync(tokenSource.Token))
            .Returns(Task.FromException<Response<QueueProperties>>(new RequestFailedException((int)HttpStatusCode.NotFound, "Queue not found")));
        mockWorkItemsQueueClient
            .Setup(c => c.Name)
            .Returns(WorkItemQueue.GetName(_taskHubInfo.TaskHubName));

        actual = await _monitor.GetUsageAsync(tokenSource.Token).ConfigureAwait(false);
        Assert.IsFalse(actual.HasActivity);
        Assert.AreEqual(0, actual.ControlQueueMessages.Count);
        Assert.AreEqual(0, actual.WorkItemQueueMessages);

        // Test missing control queue
        mockControlQueueClients[^1].Reset();
        mockControlQueueClients[^1]
            .Setup(c => c.GetPropertiesAsync(tokenSource.Token))
            .Returns(Task.FromException<Response<QueueProperties>>(new RequestFailedException((int)HttpStatusCode.NotFound, "Queue not found")));
        mockControlQueueClients[^1]
            .Setup(c => c.Name)
            .Returns(ControlQueue.GetName(_taskHubInfo.TaskHubName, mockControlQueueClients.Length - 1));

        actual = await _monitor.GetUsageAsync(tokenSource.Token).ConfigureAwait(false);
        Assert.IsFalse(actual.HasActivity);
        Assert.AreEqual(0, actual.ControlQueueMessages.Count);
        Assert.AreEqual(0, actual.WorkItemQueueMessages);
    }

    private static Action<QueueProperties, int> CreateSetter()
    {
        ParameterExpression propertiesParam = Expression.Parameter(typeof(QueueProperties), "properties");
        ParameterExpression countParam = Expression.Parameter(typeof(int), "count");

        return Expression
            .Lambda<Action<QueueProperties, int>>(
                Expression.Call(
                    propertiesParam,
                    typeof(QueueProperties).GetProperty(nameof(QueueProperties.ApproximateMessagesCount))!.SetMethod!,
                    countParam),
                propertiesParam,
                countParam)
            .Compile();
    }
}
