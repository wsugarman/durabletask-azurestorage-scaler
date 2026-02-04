// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Azure.Storage.Blobs;
using Keda.Scaler.DurableTask.AzureStorage.Clients;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Clients;

[TestClass]
public class BlobServiceClientFactoryTest : AzureStorageAccountClientFactoryTest<BlobServiceClient>
{
    protected override AzureStorageAccountClientFactory<BlobServiceClient> GetFactory()
        => new BlobServiceClientFactory();

    protected override void ValidateAccountName(BlobServiceClient actual, string accountName, string endpointSuffix)
        => Validate(actual, accountName, AzureStorageServiceUri.Create(accountName, AzureStorageService.Blob, endpointSuffix));

    protected override void ValidateEmulator(BlobServiceClient actual)
        => Validate(actual, "devstoreaccount1", new Uri("http://127.0.0.1:10000/devstoreaccount1", UriKind.Absolute));

    private static void Validate(BlobServiceClient actual, string accountName, Uri serviceUrl)
    {
        Assert.AreEqual(accountName, actual?.AccountName);
        Assert.AreEqual(serviceUrl, actual?.Uri);
    }
}
