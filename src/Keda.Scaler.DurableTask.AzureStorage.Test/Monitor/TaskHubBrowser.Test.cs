// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Keda.Scaler.DurableTask.AzureStorage.Monitor;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Monitor;

[TestClass]
public class TaskHubBrowserTest
{
    [TestMethod]
    public async Task GetAsync()
    {
        string taskHubName = "testhub";
        string leaseContainerName = $"{taskHubName}-leases";
        var serviceClient = new Mock<BlobServiceClient>();
        var blobClient = new Mock<BlobClient>();
        var containerClient = new Mock<BlobContainerClient>();
        serviceClient.Setup(x => x.GetBlobContainerClient(leaseContainerName)).Returns(containerClient.Object);
        containerClient.Setup(x => x.GetBlobClient("taskhub.json")).Returns(blobClient.Object);

        TaskHubInfo expectedInfo = new TaskHubInfo() { CreatedAt = DateTime.UtcNow, PartitionCount = 4, TaskHubName = "TestHub" };
        using var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, expectedInfo);
        stream.Seek(0, SeekOrigin.Begin);

        blobClient.Setup(x => x.OpenReadAsync(It.IsAny<long>(), It.IsAny<int?>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stream);

        ITaskHubBrowser browser = new TaskHubBrowser(serviceClient.Object, NullLogger<TaskHubBrowser>.Instance);
        var actualInfo = await browser.GetAsync(taskHubName, default).ConfigureAwait(false);
        Assert.AreEqual(expectedInfo, actualInfo);
    }
}
