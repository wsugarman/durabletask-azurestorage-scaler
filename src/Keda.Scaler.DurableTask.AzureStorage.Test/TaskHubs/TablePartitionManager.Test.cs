// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Keda.Scaler.DurableTask.AzureStorage.TaskHubs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHubs;

[TestClass]
public sealed class TablePartitionManagerTest
{
    private readonly TableClient _tableClient = Substitute.For<TableClient>();
    private readonly IOptionsSnapshot<TaskHubOptions> _optionsSnapshot = Substitute.For<IOptionsSnapshot<TaskHubOptions>>();
    private readonly TablePartitionManager _partitionManager;

    private const string TaskHubName = "UnitTest";
    private const string PartitionsTableName = "UnitTestPartitions";

    public TablePartitionManagerTest()
    {
        TableServiceClient tableServiceClient = Substitute.For<TableServiceClient>();
        _ = tableServiceClient.GetTableClient(PartitionsTableName).Returns(_tableClient);
        _ = _optionsSnapshot.Get(default).Returns(new TaskHubOptions() { TaskHubName = TaskHubName });
        _partitionManager = new TablePartitionManager(tableServiceClient, _optionsSnapshot, NullLoggerFactory.Instance);
    }

    public required TestContext TestContext { get; init; }

    [TestMethod]
    public void GivenNullClient_WhenCreatingTablePartitionManager_ThenThrowArgumentNullException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => new TablePartitionManager(null!, _optionsSnapshot, NullLoggerFactory.Instance));

    [TestMethod]
    public void GivenNullOptionsSnapshot_WhenCreatingTablePartitionManager_ThenThrowArgumentNullException()
    {
        TableServiceClient serviceClient = Substitute.For<TableServiceClient>();
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new TablePartitionManager(serviceClient, null!, NullLoggerFactory.Instance));

        IOptionsSnapshot<TaskHubOptions> nullSnapshot = Substitute.For<IOptionsSnapshot<TaskHubOptions>>();
        _ = nullSnapshot.Get(default).Returns(default(TaskHubOptions));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new TablePartitionManager(serviceClient, nullSnapshot, NullLoggerFactory.Instance));
    }

    [TestMethod]
    public void GivenNullLoggerFactory_WhenCreatingTablePartitionManager_ThenThrowArgumentNullException()
    {
        TableServiceClient serviceClient = Substitute.For<TableServiceClient>();
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new TablePartitionManager(serviceClient, _optionsSnapshot, null!));

        ILoggerFactory nullFactory = Substitute.For<ILoggerFactory>();
        _ = nullFactory.CreateLogger(default!).ReturnsForAnyArgs(default(ILogger));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new TablePartitionManager(serviceClient, _optionsSnapshot, nullFactory));
    }

    [TestMethod]
    public async ValueTask GivenEmptyTable_WhenGettingPartitions_ThenReturnEmptyList()
    {
        _ = _tableClient
            .QueryAsync<TableEntity>(default(string), default, default, TestContext.CancellationToken)
            .ReturnsForAnyArgs(AsyncPageable<TableEntity>.FromPages([]));

        using CancellationTokenSource cts = new();
        IReadOnlyList<string> actual = await _partitionManager.GetPartitionsAsync(cts.Token);

        _ = _tableClient
            .Received(1)
            .QueryAsync<TableEntity>(select: Arg.Is<IEnumerable<string>>(x => x.Single() == nameof(TableEntity.RowKey)), cancellationToken: cts.Token);

        Assert.IsEmpty(actual);
    }

    [TestMethod]
    public async ValueTask GivenTableNotFound_WhenGettingPartitions_ThenReturnEmptyList()
    {
        _ = _tableClient
            .QueryAsync<TableEntity>(default(string), default, default, TestContext.CancellationToken)
            .ThrowsForAnyArgs(new RequestFailedException((int)HttpStatusCode.NotFound, "Table not found"));

        using CancellationTokenSource cts = new();
        IReadOnlyList<string> actual = await _partitionManager.GetPartitionsAsync(cts.Token);

        _ = _tableClient
            .Received(1)
            .QueryAsync<TableEntity>(select: Arg.Is<IEnumerable<string>>(x => x.Single() == nameof(TableEntity.RowKey)), cancellationToken: cts.Token);

        Assert.IsEmpty(actual);
    }

    [TestMethod]
    public async ValueTask GivenUnexpectedTableError_WhenGettingPartitions_ThenReturnEmptyList()
    {
        RequestFailedException expected = new((int)HttpStatusCode.Unauthorized, "Unauthorized");

        _ = _tableClient
            .QueryAsync<TableEntity>(default(string), default, default, TestContext.CancellationToken)
            .ThrowsForAnyArgs(expected);

        using CancellationTokenSource cts = new();
        RequestFailedException actual = await Assert.ThrowsExactlyAsync<RequestFailedException>(() => _partitionManager.GetPartitionsAsync(cts.Token).AsTask());

        _ = _tableClient
            .Received(1)
            .QueryAsync<TableEntity>(select: Arg.Is<IEnumerable<string>>(x => x.Single() == nameof(TableEntity.RowKey)), cancellationToken: cts.Token);

        Assert.AreSame(expected, actual);
    }

    [TestMethod]
    public async ValueTask GivenTableWithRows_WhenGettingPartitions_ThenReturnPartitions()
    {
        _ = _tableClient
            .QueryAsync<TableEntity>(default(string), default, default, TestContext.CancellationToken)
            .ReturnsForAnyArgs(AsyncPageable<TableEntity>.FromPages(
                [
                    Page<TableEntity>.FromValues(
                        [
                            new TableEntity { RowKey = ControlQueue.GetName(TaskHubName, 0) },
                            new TableEntity { RowKey = ControlQueue.GetName(TaskHubName, 1) },
                        ],
                        Convert.ToBase64String([1, 2, 3, 4, 5]),
                        Substitute.For<Response>()),
                    Page<TableEntity>.FromValues(
                        [
                            new TableEntity { RowKey = ControlQueue.GetName(TaskHubName, 2) },
                        ],
                        null,
                        Substitute.For<Response>()),
                ]));

        using CancellationTokenSource cts = new();
        IReadOnlyList<string> actual = await _partitionManager.GetPartitionsAsync(cts.Token);

        _ = _tableClient
            .Received(1)
            .QueryAsync<TableEntity>(select: Arg.Is<IEnumerable<string>>(x => x.Single() == nameof(TableEntity.RowKey)), cancellationToken: cts.Token);

        string[] expected = [.. Enumerable.Repeat(TaskHubName, 3).Select(ControlQueue.GetName)];
        CollectionAssert.AreEqual(expected, actual as List<string>);
    }
}
