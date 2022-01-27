// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Cloud;

[TestClass]
public class AzureClientFactoryTest
{
    [TestMethod]
    public void GetBlobServiceClient()
    {
        // A valid account key 
        string accountKey = "/J3m0VCxNztyamCFlEXKMggO0SGZc1kB2Z7UksJkm1SS7Eyf0aUuTyRS3Q4lQzNix4Usx6BGwfIq3sDEwa+I5w==";
        string accountName = "testaccount";
        var blobClientOptions = new BlobClientOptions();
        var queueClientOptions = new QueueClientOptions();
        var blobSnapshot = new Mock<IOptionsSnapshot<BlobClientOptions>>();
        blobSnapshot.Setup(x => x.Value).Returns(blobClientOptions);

        var queueSnapshot = new Mock<IOptionsSnapshot<QueueClientOptions>>();
        queueSnapshot.Setup(x => x.Value).Returns(queueClientOptions);

        AzureClientFactory factory = new AzureClientFactory(blobSnapshot.Object, queueSnapshot.Object);
        var blobClient = factory.GetBlobServiceClient($"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={accountKey};EndpointSuffix=core.windows.net;");
        Assert.AreEqual(accountName, blobClient.AccountName);
        Assert.AreEqual(new Uri("https://testaccount.blob.core.windows.net/"), blobClient.Uri);

        blobClient = factory.GetBlobServiceClient(accountName, CloudEndpoints.Public);
        Assert.AreEqual(accountName, blobClient.AccountName);
        Assert.AreEqual(new Uri("https://testaccount.blob.core.windows.net/"), blobClient.Uri);
    }

    [TestMethod]
    public void GetQueueServiceClient()
    {
        // // A valid account key
        string accountKey = "/J3m0VCxNztyamCFlEXKMggO0SGZc1kB2Z7UksJkm1SS7Eyf0aUuTyRS3Q4lQzNix4Usx6BGwfIq3sDEwa+I5w==";
        string accountName = "testaccount";
        var blobClientOptions = new BlobClientOptions();
        var queueClientOptions = new QueueClientOptions();
        var blobSnapshot = new Mock<IOptionsSnapshot<BlobClientOptions>>();
        blobSnapshot.Setup(x => x.Value).Returns(blobClientOptions);

        var queueSnapshot = new Mock<IOptionsSnapshot<QueueClientOptions>>();
        queueSnapshot.Setup(x => x.Value).Returns(queueClientOptions);

        AzureClientFactory factory = new AzureClientFactory(blobSnapshot.Object, queueSnapshot.Object);
        var queueClient = factory.GetQueueServiceClient($"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={accountKey};EndpointSuffix=core.windows.net;");
        Assert.AreEqual(accountName, queueClient.AccountName);
        Assert.AreEqual(new Uri("https://testaccount.queue.core.windows.net/"), queueClient.Uri);

        queueClient = factory.GetQueueServiceClient(accountName, CloudEndpoints.Public);
        Assert.AreEqual(accountName, queueClient.AccountName);
        Assert.AreEqual(new Uri("https://testaccount.queue.core.windows.net/"), queueClient.Uri);
    }
}
