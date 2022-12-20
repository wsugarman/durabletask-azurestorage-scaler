// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Linq.Expressions;
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
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Keda.Scaler.DurableTask.AzureStorage.TaskHub;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHub;

public class AzureStorageTaskHubBrowserTest
{
    private readonly Mock<IStorageAccountClientFactory<BlobServiceClient>> _blobServiceClientFactory;
    private readonly Mock<IStorageAccountClientFactory<QueueServiceClient>> _queueServiceClientFactory;
    private readonly AzureStorageTaskHubBrowser _browser;

    private static readonly Action<BlobDownloadResult, BinaryData> BinaryDataSetter = CreateSetter();

    public AzureStorageTaskHubBrowserTest()
    {
        _blobServiceClientFactory = new Mock<IStorageAccountClientFactory<BlobServiceClient>>(MockBehavior.Strict);
        _queueServiceClientFactory = new Mock<IStorageAccountClientFactory<QueueServiceClient>>(MockBehavior.Strict);
        _browser = new AzureStorageTaskHubBrowser(
            _blobServiceClientFactory.Object,
            _queueServiceClientFactory.Object,
            NullLoggerFactory.Instance);
    }

    [TestMethod]
    public void CtorExceptions()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new AzureStorageTaskHubBrowser(null!, _queueServiceClientFactory.Object, NullLoggerFactory.Instance));
        Assert.ThrowsException<ArgumentNullException>(() => new AzureStorageTaskHubBrowser(_blobServiceClientFactory.Object, null!, NullLoggerFactory.Instance));
        Assert.ThrowsException<ArgumentNullException>(() => new AzureStorageTaskHubBrowser(_blobServiceClientFactory.Object, _queueServiceClientFactory.Object, null!));

        Mock<ILoggerFactory> mockFactory = new Mock<ILoggerFactory>();
        mockFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns<ILogger>(null);
        Assert.ThrowsException<ArgumentNullException>(() => new AzureStorageTaskHubBrowser(_blobServiceClientFactory.Object, _queueServiceClientFactory.Object, mockFactory.Object));
    }

    [TestMethod]
    public async Task GetMonitorAsync()
    {
        const string TaskHubName = "unit-test";
        AzureStorageAccountInfo accountInfo = new AzureStorageAccountInfo
        {
            AccountName = "testaccount",
            CloudEnvironment = CloudEnvironment.AzurePublicCloud
        };
        AzureStorageTaskHubInfo taskHubInfo = new AzureStorageTaskHubInfo
        {
            CreatedAt = DateTime.UtcNow,
            PartitionCount = 4,
            TaskHubName = TaskHubName,
        };

        BlobDownloadResult result = Activator.CreateInstance<BlobDownloadResult>();
        BinaryDataSetter(result, new BinaryData(JsonSerializer.Serialize(taskHubInfo)));

        using CancellationTokenSource tokenSource = new CancellationTokenSource();

        // Set up
        Mock<BlobServiceClient> blobServiceClient = new Mock<BlobServiceClient>(MockBehavior.Strict);
        Mock<BlobContainerClient> containerClient = new Mock<BlobContainerClient>(MockBehavior.Strict);
        Mock<BlobClient> blobClient = new Mock<BlobClient>(MockBehavior.Strict);
        _blobServiceClientFactory
            .Setup(f => f.GetServiceClient(accountInfo))
            .Returns(blobServiceClient.Object);
        blobServiceClient
            .Setup(c => c.GetBlobContainerClient(LeasesContainer.GetName(TaskHubName)))
            .Returns(containerClient.Object);
        containerClient
            .Setup(c => c.GetBlobClient(LeasesContainer.TaskHubBlobName))
            .Returns(blobClient.Object);
        blobClient
            .Setup(c => c.DownloadContentAsync(tokenSource.Token))
            .Returns(Task.FromResult(Response.FromValue(result, null!)));

        Mock<QueueServiceClient> queueServiceClient = new Mock<QueueServiceClient>(MockBehavior.Strict);
        _queueServiceClientFactory
            .Setup(f => f.GetServiceClient(accountInfo))
            .Returns(queueServiceClient.Object);

        // Test successful retrieval
        ITaskHubQueueMonitor monitor = await _browser.GetMonitorAsync(accountInfo, TaskHubName, tokenSource.Token).ConfigureAwait(false);
        Assert.IsInstanceOfType<TaskHubQueueMonitor>(monitor);

        // Test unsuccessful retrieval
        blobClient.Reset();
        blobClient
            .Setup(c => c.DownloadContentAsync(tokenSource.Token))
            .Returns(Task.FromException<Response<BlobDownloadResult>>(new RequestFailedException((int)HttpStatusCode.NotFound, "Blob not found")));

        monitor = await _browser.GetMonitorAsync(accountInfo, TaskHubName, tokenSource.Token).ConfigureAwait(false);
        Assert.IsInstanceOfType<NullTaskHubQueueMonitor>(monitor);
    }

    private static Action<BlobDownloadResult, BinaryData> CreateSetter()
    {
        ParameterExpression resultsParam = Expression.Parameter(typeof(BlobDownloadResult), "properties");
        ParameterExpression binaryParam = Expression.Parameter(typeof(BinaryData), "count");

        return Expression
            .Lambda<Action<BlobDownloadResult, BinaryData>>(
                Expression.Call(
                    resultsParam,
                    typeof(BlobDownloadResult).GetProperty(nameof(BlobDownloadResult.Content))!.SetMethod!,
                    binaryParam),
                resultsParam,
                binaryParam)
            .Compile();
    }
}
