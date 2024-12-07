// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Keda.Scaler.DurableTask.AzureStorage.TaskHub;
using Keda.Scaler.DurableTask.AzureStorage.Test.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using Xunit.Abstractions;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHub;

public sealed class BlobPartitionManagerTest : IDisposable
{
    private readonly BlobServiceClient _blobServiceClient = Substitute.For<BlobServiceClient>();
    private readonly BlobContainerClient _blobContainerClient = Substitute.For<BlobContainerClient>();
    private readonly BlobClient _blobClient = Substitute.For<BlobClient>();
    private readonly IOptionsSnapshot<TaskHubOptions> _optionsSnapshot = Substitute.For<IOptionsSnapshot<TaskHubOptions>>();
    private readonly ILoggerFactory _loggerFactory;
    private readonly BlobPartitionManager _partitionManager;

    private const string TaskHubName = "UnitTestTaskHub";
    private static readonly Func<BinaryData, BlobDownloadResult> BlobDownloadResultFactory = CreateBlobDownloadResultFactory();

    public BlobPartitionManagerTest(ITestOutputHelper outputHelper)
    {
        _ = _blobServiceClient.GetBlobContainerClient(default!).ReturnsForAnyArgs(_blobContainerClient);
        _ = _blobContainerClient.GetBlobClient(default!).ReturnsForAnyArgs(_blobClient);
        _ = _optionsSnapshot.Get(default).Returns(new TaskHubOptions { TaskHubName = TaskHubName });
        _loggerFactory = XUnitLogger.CreateFactory(outputHelper);
        _partitionManager = new(_blobServiceClient, _optionsSnapshot, _loggerFactory);
    }

    public void Dispose()
        => _loggerFactory.Dispose();

    [Fact]
    public void GivenNullBlobServiceClient_WhenCreatingBlobPartitionManager_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new BlobPartitionManager(null!, _optionsSnapshot, _loggerFactory));

    [Fact]
    public void GivenNullOptions_WhenCreatingBlobPartitionManager_ThenThrowArgumentNullException()
    {
        _ = Assert.Throws<ArgumentNullException>(() => new BlobPartitionManager(_blobServiceClient, null!, _loggerFactory));

        IOptionsSnapshot<TaskHubOptions> nullSnapshot = Substitute.For<IOptionsSnapshot<TaskHubOptions>>();
        _ = nullSnapshot.Get(default).Returns(default(TaskHubOptions));
        _ = Assert.Throws<ArgumentNullException>(() => new BlobPartitionManager(_blobServiceClient, nullSnapshot, _loggerFactory));
    }

    [Fact]
    public void GivenNullLoggerFactory_WhenCreatingBlobPartitionManager_ThenThrowArgumentNullException()
    {
        _ = Assert.Throws<ArgumentNullException>(() => new BlobPartitionManager(_blobServiceClient, _optionsSnapshot, null!));

        ILoggerFactory nullFactory = Substitute.For<ILoggerFactory>();
        _ = nullFactory.CreateLogger(default!).ReturnsForAnyArgs((ILogger)null!);
        _ = Assert.Throws<ArgumentNullException>(() => new BlobPartitionManager(_blobServiceClient, _optionsSnapshot, nullFactory));
    }

    [Fact]
    public async Task GivenNullTaskHubMetadata_WhenGettingPartitions_ThenReturnEmptyEnumerable()
    {
        using CancellationTokenSource tokenSource = new();

        BlobDownloadResult downloadResult = BlobDownloadResultFactory(BinaryData.FromString("null"));
        Response<BlobDownloadResult> response = Response.FromValue(downloadResult, Substitute.For<Response>());
        _ = _blobClient.DownloadContentAsync(default).ReturnsForAnyArgs(Task.FromResult(response));

        List<string> actual = await _partitionManager.GetPartitionsAsync(tokenSource.Token).ToListAsync();

        _ = _blobServiceClient.Received(1).GetBlobContainerClient(LeasesContainer.GetName(TaskHubName));
        _ = _blobContainerClient.Received(1).GetBlobClient(LeasesContainer.TaskHubBlobName);
        _ = await _blobClient.Received(1).DownloadContentAsync(tokenSource.Token);

        Assert.Empty(actual);
    }

    [Fact]
    public async Task GivenMissingTaskHubMetadata_WhenGettingPartitions_ThenReturnNullMonitor()
    {
        using CancellationTokenSource tokenSource = new();

        _ = _blobClient
            .DownloadContentAsync(default)
            .ThrowsAsyncForAnyArgs(new RequestFailedException((int)HttpStatusCode.NotFound, "Blob not found"));

        List<string> actual = await _partitionManager.GetPartitionsAsync(tokenSource.Token).ToListAsync();

        _ = _blobServiceClient.Received(1).GetBlobContainerClient(LeasesContainer.GetName(TaskHubName));
        _ = _blobContainerClient.Received(1).GetBlobClient(LeasesContainer.TaskHubBlobName);
        _ = await _blobClient.Received(1).DownloadContentAsync(tokenSource.Token);

        Assert.Empty(actual);
    }

    [Fact]
    public async Task GivenValidTaskHub_WhenGettingMonitor_ThenReturnMonitor()
    {
        const string TaskHubName = "UnitTest";
        using CancellationTokenSource tokenSource = new();

        string json = JsonSerializer.Serialize(new
        {
            CreatedAt = DateTimeOffset.UtcNow,
            PartitionCount = 4,
            TaskHubName,
        });

        BlobDownloadResult downloadResult = BlobDownloadResultFactory(BinaryData.FromString(json));
        Response<BlobDownloadResult> response = Response.FromValue(downloadResult, Substitute.For<Response>());
        _ = _blobClient.DownloadContentAsync(default).ReturnsForAnyArgs(Task.FromResult(response));

        List<string> actual = await _partitionManager.GetPartitionsAsync(tokenSource.Token).ToListAsync();

        _ = _blobServiceClient.Received(1).GetBlobContainerClient(LeasesContainer.GetName(TaskHubName));
        _ = _blobContainerClient.Received(1).GetBlobClient(LeasesContainer.TaskHubBlobName);
        _ = await _blobClient.Received(1).DownloadContentAsync(tokenSource.Token);

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
