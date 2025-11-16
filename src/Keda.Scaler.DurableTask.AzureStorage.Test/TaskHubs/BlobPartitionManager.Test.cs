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
using Keda.Scaler.DurableTask.AzureStorage.TaskHubs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHubs;

[TestClass]
public sealed class BlobPartitionManagerTest
{
    private readonly TestContext _testContext;
    private readonly BlobClient _blobClient;
    private readonly IOptionsSnapshot<TaskHubOptions> _optionsSnapshot;
    private readonly BlobPartitionManager _partitionManager;

    private const string TaskHubName = "UnitTest";
    private static readonly Func<BinaryData, BlobDownloadResult> BlobDownloadResultFactory = CreateBlobDownloadResultFactory();

    public BlobPartitionManagerTest(TestContext testContext)
    {
        ArgumentNullException.ThrowIfNull(testContext);

        _testContext = testContext;
        _blobClient = Substitute.For<BlobClient>();
        _optionsSnapshot = Substitute.For<IOptionsSnapshot<TaskHubOptions>>();

        BlobServiceClient blobServiceClient = Substitute.For<BlobServiceClient>();
        BlobContainerClient blobContainerClient = Substitute.For<BlobContainerClient>();

        _ = blobServiceClient.GetBlobContainerClient(default!).ReturnsForAnyArgs(blobContainerClient);
        _ = blobContainerClient.GetBlobClient(default!).ReturnsForAnyArgs(_blobClient);
        _ = _optionsSnapshot.Get(default).Returns(new TaskHubOptions { TaskHubName = TaskHubName });
        _partitionManager = new(blobServiceClient, _optionsSnapshot, NullLoggerFactory.Instance);
    }

    [TestMethod]
    public void GivenNullClient_WhenCreatingBlobPartitionManager_ThenThrowArgumentNullException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => new BlobPartitionManager(null!, _optionsSnapshot, NullLoggerFactory.Instance));

    [TestMethod]
    public void GivenNullOptionsSnapshot_WhenCreatingBlobPartitionManager_ThenThrowArgumentNullException()
    {
        BlobServiceClient serviceClient = Substitute.For<BlobServiceClient>();
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new BlobPartitionManager(serviceClient, null!, NullLoggerFactory.Instance));

        IOptionsSnapshot<TaskHubOptions> nullSnapshot = Substitute.For<IOptionsSnapshot<TaskHubOptions>>();
        _ = nullSnapshot.Get(default).Returns(default(TaskHubOptions));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new BlobPartitionManager(serviceClient, nullSnapshot, NullLoggerFactory.Instance));
    }

    [TestMethod]
    public void GivenNullLoggerFactory_WhenCreatingBlobPartitionManager_ThenThrowArgumentNullException()
    {
        BlobServiceClient serviceClient = Substitute.For<BlobServiceClient>();
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new BlobPartitionManager(serviceClient, _optionsSnapshot, null!));

        ILoggerFactory nullFactory = Substitute.For<ILoggerFactory>();
        _ = nullFactory.CreateLogger(default!).ReturnsForAnyArgs(default(ILogger));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new BlobPartitionManager(serviceClient, _optionsSnapshot, nullFactory));
    }

    [TestMethod]
    public async ValueTask GivenNullTaskHubMetadata_WhenGettingPartitions_ThenReturnEmptyList()
    {
        BlobDownloadResult downloadResult = BlobDownloadResultFactory(BinaryData.FromString("null"));
        Response<BlobDownloadResult> response = Response.FromValue(downloadResult, Substitute.For<Response>());
        _ = _blobClient.DownloadContentAsync(_testContext.CancellationToken).ReturnsForAnyArgs(Task.FromResult(response));

        using CancellationTokenSource cts = new();
        IReadOnlyList<string> actual = await _partitionManager.GetPartitionsAsync(cts.Token);

        _ = await _blobClient.Received(1).DownloadContentAsync(cts.Token);
        Assert.IsEmpty(actual);
    }

    [TestMethod]
    public async ValueTask GivenMissingTaskHubMetadata_WhenGettingPartitions_ThenReturnEmptyList()
    {
        _ = _blobClient
            .DownloadContentAsync(_testContext.CancellationToken)
            .ThrowsAsyncForAnyArgs(new RequestFailedException((int)HttpStatusCode.NotFound, "Blob not found"));

        using CancellationTokenSource cts = new();
        IReadOnlyList<string> actual = await _partitionManager.GetPartitionsAsync(cts.Token);

        _ = await _blobClient.Received(1).DownloadContentAsync(cts.Token);
        Assert.IsEmpty(actual);
    }

    [TestMethod]
    public async ValueTask GivenUnexpectedBlobError_WhenGettingPartitions_ThenReturnEmptyList()
    {
        RequestFailedException expected = new((int)HttpStatusCode.Unauthorized, "Unauthorized");

        _ = _blobClient
            .DownloadContentAsync(_testContext.CancellationToken)
            .ThrowsAsyncForAnyArgs(expected);

        using CancellationTokenSource cts = new();
        RequestFailedException actual = await Assert.ThrowsExactlyAsync<RequestFailedException>(() => _partitionManager.GetPartitionsAsync(cts.Token).AsTask());

        _ = await _blobClient.Received(1).DownloadContentAsync(cts.Token);
        Assert.AreSame(expected, actual);
    }

    [TestMethod]
    public async ValueTask GivenValidTaskHub_WhenGettingPartitions_ThenReturnPartitions()
    {
        string json = JsonSerializer.Serialize(new
        {
            CreatedAt = DateTimeOffset.UtcNow,
            PartitionCount = 4,
            TaskHubName,
        });

        BlobDownloadResult downloadResult = BlobDownloadResultFactory(BinaryData.FromString(json));
        Response<BlobDownloadResult> response = Response.FromValue(downloadResult, Substitute.For<Response>());
        _ = _blobClient.DownloadContentAsync(_testContext.CancellationToken).ReturnsForAnyArgs(Task.FromResult(response));

        using CancellationTokenSource cts = new();
        IReadOnlyList<string> actual = await _partitionManager.GetPartitionsAsync(cts.Token);

        _ = await _blobClient.Received(1).DownloadContentAsync(cts.Token);
        string[] expected = [.. Enumerable.Repeat(TaskHubName, 4).Select(ControlQueue.GetName)];
        CollectionAssert.AreEqual(expected, actual as List<string>);
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
