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
using Keda.Scaler.DurableTask.AzureStorage.TaskHub;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHub;

public class TablePartitionManagerTest
{
    private readonly TableClient _tableClient = Substitute.For<TableClient>();
    private readonly TaskHubOptions _options = new() { TaskHubName = TaskHubName };
    private readonly TablePartitionManager _tablePartitionManager;

    private const string TaskHubName = "UnitTest";
    private const string PartitionsTableName = "UnitTestPartitions";

    public TablePartitionManagerTest()
    {
        TableServiceClient tableServiceClient = Substitute.For<TableServiceClient>();
        _ = tableServiceClient.GetTableClient(PartitionsTableName).Returns(_tableClient);

        IOptionsSnapshot<TaskHubOptions> options = Substitute.For<IOptionsSnapshot<TaskHubOptions>>();
        _ = options.Get(default).Returns(_options);

        _tablePartitionManager = new TablePartitionManager(tableServiceClient, options, NullLoggerFactory.Instance);
    }

    [Fact]
    public async Task GivenEmptyTable_WhenGettingPartitions_ThenReturnEmptyList()
    {
        _ = _tableClient
            .QueryAsync<TableEntity>(default(string), default, default, default)
            .ReturnsForAnyArgs(AsyncPageable<TableEntity>.FromPages([]));

        using CancellationTokenSource cts = new();
        IReadOnlyList<string> actual = await _tablePartitionManager.GetPartitionsAsync(cts.Token);

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
            .ThrowsForAnyArgs(new RequestFailedException((int)HttpStatusCode.NotFound, "Table not found", null));

        using CancellationTokenSource cts = new();
        IReadOnlyList<string> actual = await _tablePartitionManager.GetPartitionsAsync(cts.Token);

        _ = _tableClient
            .Received(1)
            .QueryAsync<TableEntity>(select: Arg.Is<IEnumerable<string>>(x => x.Single() == nameof(TableEntity.RowKey)), cancellationToken: cts.Token);

        Assert.Empty(actual);
    }

    [Fact]
    public async Task GivenUnknownTableError_WhenGettingPartitions_ThenReturnEmptyList()
    {
        RequestFailedException expected = new((int)HttpStatusCode.Unauthorized, "Unauthorized", null);

        _ = _tableClient
            .QueryAsync<TableEntity>(default(string), default, default, default)
            .ThrowsForAnyArgs(expected);

        using CancellationTokenSource cts = new();
        RequestFailedException actual = await Assert.ThrowsAsync<RequestFailedException>(() => _tablePartitionManager.GetPartitionsAsync(cts.Token).AsTask());

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
        IReadOnlyList<string> actual = await _tablePartitionManager.GetPartitionsAsync(cts.Token);

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
