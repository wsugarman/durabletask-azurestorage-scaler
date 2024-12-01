// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Keda.Scaler.DurableTask.AzureStorage.Accounts;
using Keda.Scaler.DurableTask.AzureStorage.Clients;
using Keda.Scaler.DurableTask.AzureStorage.TaskHub;
using Keda.Scaler.DurableTask.AzureStorage.Test.Logging;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using Xunit.Abstractions;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHub;

public sealed class AzureStorageTaskHubClientTest : IDisposable
{
    private readonly IStorageAccountClientFactory<BlobServiceClient> _blobServiceClientFactory = Substitute.For<IStorageAccountClientFactory<BlobServiceClient>>();
    private readonly IStorageAccountClientFactory<QueueServiceClient> _queueServiceClientFactory = Substitute.For<IStorageAccountClientFactory<QueueServiceClient>>();
    private readonly BlobServiceClient _blobServiceClient = Substitute.For<BlobServiceClient>();
    private readonly BlobContainerClient _blobContainerClient = Substitute.For<BlobContainerClient>();
    private readonly BlobClient _blobClient = Substitute.For<BlobClient>();
    private readonly QueueServiceClient _queueServiceClient = Substitute.For<QueueServiceClient>();
    private readonly ILoggerFactory _loggerFactory;

    private static readonly Func<BinaryData, BlobDownloadResult> BlobDownloadResultFactory = CreateBlobDownloadResultFactory();

    public AzureStorageTaskHubClientTest(ITestOutputHelper outputHelper)
    {
        _ = _blobServiceClientFactory.GetServiceClient(default!).ReturnsForAnyArgs(_blobServiceClient);
        _ = _blobServiceClient.GetBlobContainerClient(default!).ReturnsForAnyArgs(_blobContainerClient);
        _ = _blobContainerClient.GetBlobClient(default!).ReturnsForAnyArgs(_blobClient);
        _ = _queueServiceClientFactory.GetServiceClient(default!).ReturnsForAnyArgs(_queueServiceClient);

        _loggerFactory = XUnitLogger.CreateFactory(outputHelper);
    }

    public void Dispose()
        => _loggerFactory.Dispose();

    [Fact]
    public void GivenNullBlobServiceClientFactory_WhenCreatingAzureStorageTaskHubClient_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new AzureStorageTaskHubClient(null!, _queueServiceClientFactory, _loggerFactory));

    [Fact]
    public void GivenNullQueueServiceClientFactory_WhenCreatingAzureStorageTaskHubClient_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new AzureStorageTaskHubClient(_blobServiceClientFactory, null!, _loggerFactory));

    [Fact]
    public void GivenNullLoggerFactory_WhenCreatingAzureStorageTaskHubClient_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new AzureStorageTaskHubClient(_blobServiceClientFactory, _queueServiceClientFactory, null!));

    [Fact]
    public void GivenNullLogger_WhenCreatingAzureStorageTaskHubClient_ThenThrowArgumentNullException()
    {
        ILoggerFactory factory = Substitute.For<ILoggerFactory>();
        _ = factory.CreateLogger(default!).ReturnsForAnyArgs((ILogger)null!);

        _ = Assert.Throws<ArgumentNullException>(() => new AzureStorageTaskHubClient(_blobServiceClientFactory, _queueServiceClientFactory, factory));
    }

    [Fact]
    public Task GivenNullAzureStorageAccountInfo_WhenGettingMonitor_ThenThrowArgumentNullException()
    {
        AzureStorageTaskHubClient client = new(_blobServiceClientFactory, _queueServiceClientFactory, _loggerFactory);
        return Assert.ThrowsAsync<ArgumentNullException>(() => client.GetMonitorAsync(null!, "foo", default).AsTask());
    }

    [Fact]
    public Task GivenNullTaskHub_WhenGettingMonitor_ThenThrowArgumentNullException()
    {
        AzureStorageTaskHubClient client = new(_blobServiceClientFactory, _queueServiceClientFactory, _loggerFactory);
        return Assert.ThrowsAsync<ArgumentNullException>(() => client.GetMonitorAsync(new AzureStorageAccountOptions(), null!, default).AsTask());
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    public Task GivenEmptyOrWhiteSpaceTaskHub_WhenGettingMonitor_ThenThrowArgumentException(string taskHub)
    {
        AzureStorageTaskHubClient client = new(_blobServiceClientFactory, _queueServiceClientFactory, _loggerFactory);
        return Assert.ThrowsAsync<ArgumentException>(() => client.GetMonitorAsync(new AzureStorageAccountOptions(), taskHub, default).AsTask());
    }

    [Fact]
    public async Task GivenNullTaskHubMetadata_WhenGettingMonitor_ThenReturnNullMonitor()
    {
        const string TaskHubName = "UnitTest";
        using CancellationTokenSource tokenSource = new();

        BlobDownloadResult downloadResult = BlobDownloadResultFactory(BinaryData.FromString("null"));
        Response<BlobDownloadResult> response = Response.FromValue(downloadResult, Substitute.For<Response>());
        _ = _blobClient.DownloadContentAsync(default).ReturnsForAnyArgs(Task.FromResult(response));

        AzureStorageAccountOptions accountInfo = new();
        AzureStorageTaskHubClient client = new(_blobServiceClientFactory, _queueServiceClientFactory, _loggerFactory);
        ITaskHub actual = await client.GetMonitorAsync(accountInfo, TaskHubName, tokenSource.Token);

        _ = _blobServiceClientFactory.Received(1).GetServiceClient(Arg.Is(accountInfo));
        _ = _blobServiceClient.Received(1).GetBlobContainerClient(Arg.Is(LeasesContainer.GetName(TaskHubName)));
        _ = _blobContainerClient.Received(1).GetBlobClient(Arg.Is(LeasesContainer.TaskHubBlobName));
        _ = await _blobClient.Received(1).DownloadContentAsync(Arg.Is(tokenSource.Token));

        Assert.Same(NullTaskHubQueueMonitor.Instance, actual);
    }

    [Fact]
    public async Task GivenMissingTaskHubMetadata_WhenGettingMonitor_ThenReturnNullMonitor()
    {
        const string TaskHubName = "UnitTest";
        using CancellationTokenSource tokenSource = new();

        _ = _blobClient
            .DownloadContentAsync(default)
            .ThrowsAsyncForAnyArgs(new RequestFailedException((int)HttpStatusCode.NotFound, "Blob not found"));

        AzureStorageAccountOptions accountInfo = new();
        AzureStorageTaskHubClient client = new(_blobServiceClientFactory, _queueServiceClientFactory, _loggerFactory);
        ITaskHub actual = await client.GetMonitorAsync(accountInfo, TaskHubName, tokenSource.Token);

        _ = _blobServiceClientFactory.Received(1).GetServiceClient(Arg.Is(accountInfo));
        _ = _blobServiceClient.Received(1).GetBlobContainerClient(Arg.Is(LeasesContainer.GetName(TaskHubName)));
        _ = _blobContainerClient.Received(1).GetBlobClient(Arg.Is(LeasesContainer.TaskHubBlobName));
        _ = await _blobClient.Received(1).DownloadContentAsync(Arg.Is(tokenSource.Token));

        Assert.Same(NullTaskHubQueueMonitor.Instance, actual);
    }

    [Fact]
    public async Task GivenValidTaskHub_WhenGettingMonitor_ThenReturnMonitor()
    {
        const string TaskHubName = "UnitTest";
        using CancellationTokenSource tokenSource = new();

        AzureStorageTaskHubInfo taskHubInfo = new(DateTimeOffset.UtcNow, 4, TaskHubName);

        BlobDownloadResult downloadResult = BlobDownloadResultFactory(BinaryData.FromString(JsonSerializer.Serialize(taskHubInfo)));
        Response<BlobDownloadResult> response = Response.FromValue(downloadResult, Substitute.For<Response>());
        _ = _blobClient.DownloadContentAsync(default).ReturnsForAnyArgs(Task.FromResult(response));

        AzureStorageAccountOptions accountInfo = new();
        AzureStorageTaskHubClient client = new(_blobServiceClientFactory, _queueServiceClientFactory, _loggerFactory);
        ITaskHub actual = await client.GetMonitorAsync(accountInfo, TaskHubName, tokenSource.Token);

        _ = _blobServiceClientFactory.Received(1).GetServiceClient(Arg.Is(accountInfo));
        _ = _blobServiceClient.Received(1).GetBlobContainerClient(Arg.Is(LeasesContainer.GetName(TaskHubName)));
        _ = _blobContainerClient.Received(1).GetBlobClient(Arg.Is(LeasesContainer.TaskHubBlobName));
        _ = await _blobClient.Received(1).DownloadContentAsync(Arg.Is(tokenSource.Token));
        _ = _queueServiceClientFactory.Received(1).GetServiceClient(Arg.Is(accountInfo));

        _ = Assert.IsType<TaskHub>(actual);
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
