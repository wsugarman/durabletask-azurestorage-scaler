// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
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
        _ = Assert.ThrowsException<ArgumentNullException>(() => new AzureStorageTaskHubBrowser(null!, _queueServiceClientFactory.Object, NullLoggerFactory.Instance));
        _ = Assert.ThrowsException<ArgumentNullException>(() => new AzureStorageTaskHubBrowser(_blobServiceClientFactory.Object, null!, NullLoggerFactory.Instance));
        _ = Assert.ThrowsException<ArgumentNullException>(() => new AzureStorageTaskHubBrowser(_blobServiceClientFactory.Object, _queueServiceClientFactory.Object, null!));

        Mock<ILoggerFactory> mockFactory = new(MockBehavior.Strict);
        _ = mockFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns<ILogger>(null!);
        _ = Assert.ThrowsException<ArgumentNullException>(() => new AzureStorageTaskHubBrowser(_blobServiceClientFactory.Object, _queueServiceClientFactory.Object, mockFactory.Object));
    }

    [TestMethod]
    [SuppressMessage("Maintainability", "CA1506:Avoid excessive class coupling", Justification = "Will refactor tests to be scoped to scenario.")]
    public async Task GetMonitorAsync()
    {
        const string TaskHubName = "unit-test";
        AzureStorageAccountInfo accountInfo = new()
        {
            AccountName = "testaccount",
            Cloud = CloudEndpoints.Public,
        };
        AzureStorageTaskHubInfo taskHubInfo = new()
        {
            CreatedAt = DateTime.UtcNow,
            PartitionCount = 4,
            TaskHubName = TaskHubName,
        };

        BlobDownloadResult result = DownloadResultFactory(new BinaryData(JsonSerializer.Serialize(taskHubInfo)));

        using CancellationTokenSource tokenSource = new();

        // Set up
        Mock<BlobServiceClient> blobServiceClient = new(MockBehavior.Strict);
        Mock<BlobContainerClient> containerClient = new(MockBehavior.Strict);
        Mock<BlobClient> blobClient = new(MockBehavior.Strict);
        _ = _blobServiceClientFactory
            .Setup(f => f.GetServiceClient(accountInfo))
            .Returns(blobServiceClient.Object);
        _ = blobServiceClient
            .Setup(c => c.GetBlobContainerClient(LeasesContainer.GetName(TaskHubName)))
            .Returns(containerClient.Object);
        _ = containerClient
            .Setup(c => c.GetBlobClient(LeasesContainer.TaskHubBlobName))
            .Returns(blobClient.Object);
        _ = blobClient
            .Setup(c => c.DownloadContentAsync(tokenSource.Token))
            .Returns(Task.FromResult(Response.FromValue(result, null!)));

        Mock<QueueServiceClient> queueServiceClient = new(MockBehavior.Strict);
        _ = _queueServiceClientFactory
            .Setup(f => f.GetServiceClient(accountInfo))
            .Returns(queueServiceClient.Object);

        // Exceptions
        _ = await Assert
            .ThrowsExceptionAsync<ArgumentNullException>(() => _browser.GetMonitorAsync(null!, TaskHubName).AsTask())
            .ConfigureAwait(false);
        _ = await Assert
            .ThrowsExceptionAsync<ArgumentNullException>(() => _browser.GetMonitorAsync(accountInfo, null!).AsTask())
            .ConfigureAwait(false);
        _ = await Assert
            .ThrowsExceptionAsync<ArgumentException>(() => _browser.GetMonitorAsync(accountInfo, "").AsTask())
            .ConfigureAwait(false);

        // Test successful retrieval
        ITaskHubQueueMonitor monitor = await _browser.GetMonitorAsync(accountInfo, TaskHubName, tokenSource.Token).ConfigureAwait(false);
        Assert.IsInstanceOfType<TaskHubQueueMonitor>(monitor);

        // Test unsuccessful retrieval
        blobClient.Reset();
        _ = blobClient
            .Setup(c => c.DownloadContentAsync(tokenSource.Token))
            .Returns(Task.FromException<Response<BlobDownloadResult>>(new RequestFailedException((int)HttpStatusCode.NotFound, "Blob not found")));
        _ = blobClient.Setup(c => c.BlobContainerName).Returns(LeasesContainer.GetName(TaskHubName));
        _ = blobClient.Setup(c => c.Name).Returns(LeasesContainer.TaskHubBlobName);

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
