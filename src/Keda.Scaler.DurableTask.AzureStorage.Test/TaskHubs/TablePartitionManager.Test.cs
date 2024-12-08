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
using Keda.Scaler.DurableTask.AzureStorage.Test.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using Xunit.Abstractions;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHubs;

public sealed class TablePartitionManagerTest : IDisposable
{
    private readonly TableClient _tableClient = Substitute.For<TableClient>();
    private readonly IOptionsSnapshot<TaskHubOptions> _optionsSnapshot = Substitute.For<IOptionsSnapshot<TaskHubOptions>>();
    private readonly ILoggerFactory _loggerFactory;
    private readonly TablePartitionManager _partitionManager;

    private const string TaskHubName = "UnitTest";
    private const string PartitionsTableName = "UnitTestPartitions";

    public TablePartitionManagerTest(ITestOutputHelper outputHelper)
    {
        TableServiceClient tableServiceClient = Substitute.For<TableServiceClient>();
        _ = tableServiceClient.GetTableClient(PartitionsTableName).Returns(_tableClient);
        _ = _optionsSnapshot.Get(default).Returns(new TaskHubOptions() { TaskHubName = TaskHubName });
        _loggerFactory = XUnitLogger.CreateFactory(outputHelper);
        _partitionManager = new TablePartitionManager(tableServiceClient, _optionsSnapshot, _loggerFactory);
    }

    public void Dispose()
        => _loggerFactory.Dispose();

    [Fact]
    public void GivenNullClient_WhenCreatingTablePartitionManager_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new TablePartitionManager(null!, _optionsSnapshot, _loggerFactory));

    [Fact]
    public void GivenNullOptionsSnapshot_WhenCreatingTablePartitionManager_ThenThrowArgumentNullException()
    {
        TableServiceClient serviceClient = Substitute.For<TableServiceClient>();
        _ = Assert.Throws<ArgumentNullException>(() => new TablePartitionManager(serviceClient, null!, _loggerFactory));

        IOptionsSnapshot<TaskHubOptions> nullSnapshot = Substitute.For<IOptionsSnapshot<TaskHubOptions>>();
        _ = nullSnapshot.Get(default).Returns(default(TaskHubOptions));
        _ = Assert.Throws<ArgumentNullException>(() => new TablePartitionManager(serviceClient, nullSnapshot, _loggerFactory));
    }

    [Fact]
    public void GivenNullLoggerFactory_WhenCreatingTablePartitionManager_ThenThrowArgumentNullException()
    {
        TableServiceClient serviceClient = Substitute.For<TableServiceClient>();
        _ = Assert.Throws<ArgumentNullException>(() => new TablePartitionManager(serviceClient, _optionsSnapshot, null!));

        ILoggerFactory nullFactory = Substitute.For<ILoggerFactory>();
        _ = nullFactory.CreateLogger(default!).ReturnsForAnyArgs(default(ILogger));
        _ = Assert.Throws<ArgumentNullException>(() => new TablePartitionManager(serviceClient, _optionsSnapshot, nullFactory));
    }

    [Fact]
    public async Task GivenEmptyTable_WhenGettingPartitions_ThenReturnEmptyList()
    {
        _ = _tableClient
            .QueryAsync<TableEntity>(default(string), default, default, default)
            .ReturnsForAnyArgs(AsyncPageable<TableEntity>.FromPages([]));

        using CancellationTokenSource cts = new();
        IReadOnlyList<string> actual = await _partitionManager.GetPartitionsAsync(cts.Token);

        _ = _tableClient
            .Received(1)
            .QueryAsync<TableEntity>(select: Arg.Is<IEnumerable<string>>(x => x.Single() == nameof(TableEntity.RowKey)), cancellationToken: cts.Token);

        Assert.Empty(actual);
    }

    [Fact]
    public async Task GivenTableNotFound_WhenGettingPartitions_ThenReturnEmptyList()
    {
        _ = _tableClient
            .QueryAsync<TableEntity>(default(string), default, default, default)
            .ThrowsForAnyArgs(new RequestFailedException((int)HttpStatusCode.NotFound, "Table not found"));

        using CancellationTokenSource cts = new();
        IReadOnlyList<string> actual = await _partitionManager.GetPartitionsAsync(cts.Token);

        _ = _tableClient
            .Received(1)
            .QueryAsync<TableEntity>(select: Arg.Is<IEnumerable<string>>(x => x.Single() == nameof(TableEntity.RowKey)), cancellationToken: cts.Token);

        Assert.Empty(actual);
    }

    [Fact]
    public async Task GivenUnexpectedTableError_WhenGettingPartitions_ThenReturnEmptyList()
    {
        RequestFailedException expected = new((int)HttpStatusCode.Unauthorized, "Unauthorized");

        _ = _tableClient
            .QueryAsync<TableEntity>(default(string), default, default, default)
            .ThrowsForAnyArgs(expected);

        using CancellationTokenSource cts = new();
        RequestFailedException actual = await Assert.ThrowsAsync<RequestFailedException>(() => _partitionManager.GetPartitionsAsync(cts.Token).AsTask());

        _ = _tableClient
            .Received(1)
            .QueryAsync<TableEntity>(select: Arg.Is<IEnumerable<string>>(x => x.Single() == nameof(TableEntity.RowKey)), cancellationToken: cts.Token);

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task GivenTableWithRows_WhenGettingPartitions_ThenReturnPartitions()
    {
        _ = _tableClient
            .QueryAsync<TableEntity>(default(string), default, default, default)
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

        Assert.Collection(
            actual,
            x => Assert.Equal(ControlQueue.GetName(TaskHubName, 0), x),
            x => Assert.Equal(ControlQueue.GetName(TaskHubName, 1), x),
            x => Assert.Equal(ControlQueue.GetName(TaskHubName, 2), x));
    }
}
