// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Keda.Scaler.DurableTask.AzureStorage.Accounts;
using Keda.Scaler.DurableTask.AzureStorage.Blobs;
using Keda.Scaler.DurableTask.AzureStorage.TaskHub;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHub;

public class AzureStorageTaskHubClientTest
{
    private readonly IStorageAccountClientFactory<BlobServiceClient> _blobServiceClientFactory = Substitute.For<IStorageAccountClientFactory<BlobServiceClient>>();
    private readonly IStorageAccountClientFactory<QueueServiceClient> _queueServiceClientFactory = Substitute.For<IStorageAccountClientFactory<QueueServiceClient>>();
    private readonly BlobServiceClient _blobServiceClient = Substitute.For<BlobServiceClient>();
    private readonly BlobContainerClient _blobContainerClient = Substitute.For<BlobContainerClient>();
    private readonly BlobClient _blobClient = Substitute.For<BlobClient>();
    private readonly QueueServiceClient _queueServiceClient = Substitute.For<QueueServiceClient>();

    public AzureStorageTaskHubClientTest()
    {
        _ = _blobServiceClientFactory.GetServiceClient(Arg.Any<AzureStorageAccountInfo>()).Returns(_blobServiceClient);
        _ = _blobServiceClient.GetBlobContainerClient(Arg.Any<string>()).Returns(_blobContainerClient);
        _ = _blobContainerClient.GetBlobClient(Arg.Any<string>()).Returns(_blobClient);

        _ = _queueServiceClientFactory.GetServiceClient(Arg.Any<AzureStorageAccountInfo>()).Returns(_queueServiceClient);
    }

    [Fact]
    public void GivenNullBlobServiceClientFactory_WhenCreatingAzureStorageTaskHubClient_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new AzureStorageTaskHubClient(null!, _queueServiceClientFactory, NullLoggerFactory.Instance));

    [Fact]
    public void GivenNullQueueServiceClientFactory_WhenCreatingAzureStorageTaskHubClient_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new AzureStorageTaskHubClient(_blobServiceClientFactory, null!, NullLoggerFactory.Instance));

    [Fact]
    public void GivenNullLoggerFactory_WhenCreatingAzureStorageTaskHubClient_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new AzureStorageTaskHubClient(_blobServiceClientFactory, _queueServiceClientFactory, null!));

    [Fact]
    public void GivenNullLogger_WhenCreatingAzureStorageTaskHubClient_ThenThrowArgumentNullException()
    {
        ILoggerFactory factory = Substitute.For<ILoggerFactory>();
        _ = factory.CreateLogger(Arg.Any<string>()).Returns((ILogger)null!);

        _ = Assert.Throws<ArgumentNullException>(() => new AzureStorageTaskHubClient(_blobServiceClientFactory, _queueServiceClientFactory, factory));
    }

    [Fact]
    public Task GivenNullAzureStorageAccountInfo_WhenGettingMonitor_ThenThrowArgumentNullException()
    {
        AzureStorageTaskHubClient client = new(_blobServiceClientFactory, _queueServiceClientFactory, NullLoggerFactory.Instance);
        return Assert.ThrowsAsync<ArgumentNullException>(() => client.GetMonitorAsync(null!, "foo", default).AsTask());
    }

    [Fact]
    public Task GivenNullTaskHub_WhenGettingMonitor_ThenThrowArgumentNullException()
    {
        AzureStorageTaskHubClient client = new(_blobServiceClientFactory, _queueServiceClientFactory, NullLoggerFactory.Instance);
        return Assert.ThrowsAsync<ArgumentNullException>(() => client.GetMonitorAsync(new AzureStorageAccountInfo(), null!, default).AsTask());
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    public Task GivenEmptyOrWhiteSpaceTaskHub_WhenGettingMonitor_ThenThrowArgumentException(string taskHub)
    {
        AzureStorageTaskHubClient client = new(_blobServiceClientFactory, _queueServiceClientFactory, NullLoggerFactory.Instance);
        return Assert.ThrowsAsync<ArgumentException>(() => client.GetMonitorAsync(new AzureStorageAccountInfo(), taskHub, default).AsTask());
    }

    [Fact]
    public async Task GivenMissingTaskHubMetadata_WhenGettingMonitor_ThenReturnNullMonitor()
    {
        const string TaskHubName = "UnitTest";
        using CancellationTokenSource tokenSource = new();

        _ = _blobClient
            .DownloadContentAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new RequestFailedException((int)HttpStatusCode.NotFound, "Blob not found"));

        AzureStorageAccountInfo accountInfo = new();
        AzureStorageTaskHubClient client = new(_blobServiceClientFactory, _queueServiceClientFactory, NullLoggerFactory.Instance);
        ITaskHubQueueMonitor actual = await client.GetMonitorAsync(accountInfo, TaskHubName, tokenSource.Token);

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

        BlobDownloadResult downloadResult = Substitute.For<BlobDownloadResult>();
        _ = downloadResult.Content.Returns(BinaryData.FromString(JsonSerializer.Serialize(taskHubInfo)));
        Response<BlobDownloadResult> response = Response.FromValue(downloadResult, Substitute.For<Response>());
        _ = _blobClient.DownloadContentAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(response));

        AzureStorageAccountInfo accountInfo = new();
        AzureStorageTaskHubClient client = new(_blobServiceClientFactory, _queueServiceClientFactory, NullLoggerFactory.Instance);
        ITaskHubQueueMonitor actual = await client.GetMonitorAsync(accountInfo, TaskHubName, tokenSource.Token);

        _ = _blobServiceClientFactory.Received(1).GetServiceClient(Arg.Is(accountInfo));
        _ = _blobServiceClient.Received(1).GetBlobContainerClient(Arg.Is(LeasesContainer.GetName(TaskHubName)));
        _ = _blobContainerClient.Received(1).GetBlobClient(Arg.Is(LeasesContainer.TaskHubBlobName));
        _ = await _blobClient.Received(1).DownloadContentAsync(Arg.Is(tokenSource.Token));
        _ = _queueServiceClientFactory.Received(1).GetServiceClient(Arg.Is(accountInfo));

        _ = Assert.IsType<TaskHubQueueMonitor>(actual);
    }
}
