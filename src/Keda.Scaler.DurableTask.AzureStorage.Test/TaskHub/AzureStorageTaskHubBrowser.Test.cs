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
using Keda.Scaler.DurableTask.AzureStorage.Blobs;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Keda.Scaler.DurableTask.AzureStorage.TaskHub;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHub;

[TestClass]
public class AzureStorageTaskHubBrowserTest
{
    private readonly Mock<IStorageAccountClientFactory<BlobServiceClient>> _blobServiceClientFactory;
    private readonly Mock<IStorageAccountClientFactory<QueueServiceClient>> _queueServiceClientFactory;
    private readonly AzureStorageTaskHubBrowser _browser;

    private static readonly Func<BinaryData, BlobDownloadResult> DownloadResultFactory = CreateFactory();

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

        Mock<ILoggerFactory> mockFactory = new Mock<ILoggerFactory>(MockBehavior.Strict);
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
            Cloud = CloudEndpoints.Public,
        };
        AzureStorageTaskHubInfo taskHubInfo = new AzureStorageTaskHubInfo
        {
            CreatedAt = DateTime.UtcNow,
            PartitionCount = 4,
            TaskHubName = TaskHubName,
        };

        BlobDownloadResult result = DownloadResultFactory(new BinaryData(JsonSerializer.Serialize(taskHubInfo)));

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

        // Exceptions
        await Assert
            .ThrowsExceptionAsync<ArgumentNullException>(() => _browser.GetMonitorAsync(null!, TaskHubName).AsTask())
            .ConfigureAwait(false);
        await Assert
            .ThrowsExceptionAsync<ArgumentNullException>(() => _browser.GetMonitorAsync(accountInfo, null!).AsTask())
            .ConfigureAwait(false);

        // Test successful retrieval
        ITaskHubQueueMonitor monitor = await _browser.GetMonitorAsync(accountInfo, TaskHubName, tokenSource.Token).ConfigureAwait(false);
        Assert.IsInstanceOfType<TaskHubQueueMonitor>(monitor);

        // Test unsuccessful retrieval
        blobClient.Reset();
        blobClient
            .Setup(c => c.DownloadContentAsync(tokenSource.Token))
            .Returns(Task.FromException<Response<BlobDownloadResult>>(new RequestFailedException((int)HttpStatusCode.NotFound, "Blob not found")));
        blobClient.Setup(c => c.BlobContainerName).Returns(LeasesContainer.GetName(TaskHubName));
        blobClient.Setup(c => c.Name).Returns(LeasesContainer.TaskHubBlobName);

        monitor = await _browser.GetMonitorAsync(accountInfo, TaskHubName, tokenSource.Token).ConfigureAwait(false);
        Assert.IsInstanceOfType<NullTaskHubQueueMonitor>(monitor);
    }

    private static Func<BinaryData, BlobDownloadResult> CreateFactory()
    {
        ParameterExpression binaryParam = Expression.Parameter(typeof(BinaryData), "binaryData");
        ParameterExpression resultsVar = Expression.Variable(typeof(BlobDownloadResult), "results");

        return Expression
            .Lambda<Func<BinaryData, BlobDownloadResult>>(
                Expression.Block(
                    typeof(BlobDownloadResult),
                    new ParameterExpression[] { resultsVar },
                    Expression.Assign(
                        resultsVar,
                        Expression.New(
                            typeof(BlobDownloadResult).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, Type.EmptyTypes)!)),
                    Expression.Call(
                        resultsVar,
                        typeof(BlobDownloadResult).GetProperty(nameof(BlobDownloadResult.Content))!.GetSetMethod(nonPublic: true)!,
                        binaryParam),
                    resultsVar),
                binaryParam)
            .Compile();
    }
}
