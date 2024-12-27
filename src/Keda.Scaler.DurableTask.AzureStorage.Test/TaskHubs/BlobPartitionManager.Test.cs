// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Keda.Scaler.DurableTask.AzureStorage.TaskHubs;
using Keda.Scaler.DurableTask.AzureStorage.Test.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using Xunit.Abstractions;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHubs;

public sealed class BlobPartitionManagerTest : IDisposable
{
    private readonly BlobClient _blobClient = Substitute.For<BlobClient>();
    private readonly IOptionsSnapshot<TaskHubOptions> _optionsSnapshot = Substitute.For<IOptionsSnapshot<TaskHubOptions>>();
    private readonly ILoggerFactory _loggerFactory;
    private readonly BlobPartitionManager _partitionManager;

    private const string TaskHubName = "UnitTest";
    private static readonly Func<BinaryData, BlobDownloadResult> BlobDownloadResultFactory = CreateBlobDownloadResultFactory();

    public BlobPartitionManagerTest(ITestOutputHelper outputHelper)
    {
        BlobServiceClient blobServiceClient = Substitute.For<BlobServiceClient>();
        BlobContainerClient blobContainerClient = Substitute.For<BlobContainerClient>();

        _ = blobServiceClient.GetBlobContainerClient(default!).ReturnsForAnyArgs(blobContainerClient);
        _ = blobContainerClient.GetBlobClient(default!).ReturnsForAnyArgs(_blobClient);
        _ = _optionsSnapshot.Get(default).Returns(new TaskHubOptions { TaskHubName = TaskHubName });
        _loggerFactory = XUnitLogger.CreateFactory(outputHelper);
        _partitionManager = new(blobServiceClient, _optionsSnapshot, _loggerFactory);
    }

    public void Dispose()
        => _loggerFactory.Dispose();

    [Fact]
    public void GivenNullClient_WhenCreatingBlobPartitionManager_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new BlobPartitionManager(null!, _optionsSnapshot, _loggerFactory));

    [Fact]
    public void GivenNullOptionsSnapshot_WhenCreatingBlobPartitionManager_ThenThrowArgumentNullException()
    {
        BlobServiceClient serviceClient = Substitute.For<BlobServiceClient>();
        _ = Assert.Throws<ArgumentNullException>(() => new BlobPartitionManager(serviceClient, null!, _loggerFactory));

        IOptionsSnapshot<TaskHubOptions> nullSnapshot = Substitute.For<IOptionsSnapshot<TaskHubOptions>>();
        _ = nullSnapshot.Get(default).Returns(default(TaskHubOptions));
        _ = Assert.Throws<ArgumentNullException>(() => new BlobPartitionManager(serviceClient, nullSnapshot, _loggerFactory));
    }

    [Fact]
    public void GivenNullLoggerFactory_WhenCreatingBlobPartitionManager_ThenThrowArgumentNullException()
    {
        BlobServiceClient serviceClient = Substitute.For<BlobServiceClient>();
        _ = Assert.Throws<ArgumentNullException>(() => new BlobPartitionManager(serviceClient, _optionsSnapshot, null!));

        ILoggerFactory nullFactory = Substitute.For<ILoggerFactory>();
        _ = nullFactory.CreateLogger(default!).ReturnsForAnyArgs(default(ILogger));
        _ = Assert.Throws<ArgumentNullException>(() => new BlobPartitionManager(serviceClient, _optionsSnapshot, nullFactory));
    }

    [Fact]
    public async Task GivenNullTaskHubMetadata_WhenGettingPartitions_ThenReturnEmptyList()
    {
        BlobDownloadResult downloadResult = BlobDownloadResultFactory(BinaryData.FromString("null"));
        Response<BlobDownloadResult> response = Response.FromValue(downloadResult, Substitute.For<Response>());
        _ = _blobClient.DownloadContentAsync(default).ReturnsForAnyArgs(Task.FromResult(response));

        using CancellationTokenSource cts = new();
        IReadOnlyList<string> actual = await _partitionManager.GetPartitionsAsync(cts.Token);

        _ = await _blobClient.Received(1).DownloadContentAsync(cts.Token);
        Assert.Empty(actual);
    }

    [Fact]
    public async Task GivenMissingTaskHubMetadata_WhenGettingPartitions_ThenReturnEmptyList()
    {
        _ = _blobClient
            .DownloadContentAsync(default)
            .ThrowsAsyncForAnyArgs(new RequestFailedException((int)HttpStatusCode.NotFound, "Blob not found"));

        using CancellationTokenSource cts = new();
        IReadOnlyList<string> actual = await _partitionManager.GetPartitionsAsync(cts.Token);

        _ = await _blobClient.Received(1).DownloadContentAsync(cts.Token);
        Assert.Empty(actual);
    }

    [Fact]
    public async Task GivenUnexpectedBlobError_WhenGettingPartitions_ThenReturnEmptyList()
    {
        RequestFailedException expected = new((int)HttpStatusCode.Unauthorized, "Unauthorized");

        _ = _blobClient
            .DownloadContentAsync(default)
            .ThrowsAsyncForAnyArgs(expected);

        using CancellationTokenSource cts = new();
        RequestFailedException actual = await Assert.ThrowsAsync<RequestFailedException>(() => _partitionManager.GetPartitionsAsync(cts.Token).AsTask());

        _ = await _blobClient.Received(1).DownloadContentAsync(cts.Token);
        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task GivenValidTaskHub_WhenGettingPartitions_ThenReturnPartitions()
    {
        string json = JsonSerializer.Serialize(new
        {
            CreatedAt = DateTimeOffset.UtcNow,
            PartitionCount = 4,
            TaskHubName,
        });

        BlobDownloadResult downloadResult = BlobDownloadResultFactory(BinaryData.FromString(json));
        Response<BlobDownloadResult> response = Response.FromValue(downloadResult, Substitute.For<Response>());
        _ = _blobClient.DownloadContentAsync(default).ReturnsForAnyArgs(Task.FromResult(response));

        using CancellationTokenSource cts = new();
        IReadOnlyList<string> actual = await _partitionManager.GetPartitionsAsync(cts.Token);

        _ = await _blobClient.Received(1).DownloadContentAsync(cts.Token);
        Assert.Collection(
            actual,
            x => Assert.Equal(ControlQueue.GetName(TaskHubName, 0), x),
            x => Assert.Equal(ControlQueue.GetName(TaskHubName, 1), x),
            x => Assert.Equal(ControlQueue.GetName(TaskHubName, 2), x),
            x => Assert.Equal(ControlQueue.GetName(TaskHubName, 3), x));
    }

    private static Func<BinaryData, BlobDownloadResult> CreateBlobDownloadResultFactory()
    {
        ParameterExpression param = Expression.Parameter(typeof(BinaryData), "data");
        ParameterExpression variable = Expression.Variable(typeof(BlobDownloadResult), "result");
        MethodInfo? contentSetter = typeof(BlobDownloadResult)
            .GetProperty(nameof(BlobDownloadResult.Content))?
            .GetSetMethod(nonPublic: true);

        return (Func<BinaryData, BlobDownloadResult>)Expression
            .Lambda(
                Expression.Block(
                    typeof(BlobDownloadResult),
                    [variable],
                    Expression.Assign(variable, Expression.New(typeof(BlobDownloadResult))),
                    Expression.Call(variable, contentSetter!, param),
                    variable),
                param)
            .Compile();
    }
}
