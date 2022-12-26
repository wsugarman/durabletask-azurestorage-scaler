// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Azure.Storage.Blobs;
using Keda.Scaler.DurableTask.AzureStorage.Accounts;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Keda.Scaler.DurableTask.AzureStorage.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Accounts;

[TestClass]
public class BlobServiceClientFactoryTest : AzureStorageAccountClientFactoryTest<BlobServiceClient>
{
    protected override IStorageAccountClientFactory<BlobServiceClient> GetFactory()
        => new BlobServiceClientFactory();

    protected override void ValidateAccountName(BlobServiceClient actual, string accountName, CloudEndpoints cloud)
        => Validate(actual, accountName, cloud.GetStorageServiceUri(accountName, AzureStorageService.Blob));

    protected override void ValidateEmulator(BlobServiceClient actual)
        => Validate(actual, "devstoreaccount1", new Uri("http://127.0.0.1:10000/devstoreaccount1", UriKind.Absolute));

    private static void Validate(BlobServiceClient actual, string accountName, Uri serviceUrl)
    {
        Assert.AreEqual(accountName, actual?.AccountName);
        Assert.AreEqual(serviceUrl, actual?.Uri);
    }
}
