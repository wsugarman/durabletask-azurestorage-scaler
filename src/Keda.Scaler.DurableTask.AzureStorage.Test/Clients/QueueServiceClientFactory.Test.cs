// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Azure.Storage.Queues;
using Keda.Scaler.DurableTask.AzureStorage.Clients;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Clients;

[TestClass]
public class QueueServiceClientFactoryTest : AzureStorageAccountClientFactoryTest<QueueServiceClient>
{
    protected override AzureStorageAccountClientFactory<QueueServiceClient> GetFactory()
        => new QueueServiceClientFactory();

    protected override void ValidateAccountName(QueueServiceClient actual, string accountName, string endpointSuffix)
        => Validate(actual, accountName, AzureStorageServiceUri.Create(accountName, AzureStorageService.Queue, endpointSuffix));

    protected override void ValidateEmulator(QueueServiceClient actual)
        => Validate(actual, "devstoreaccount1", new Uri("http://127.0.0.1:10001/devstoreaccount1", UriKind.Absolute));

    private static void Validate(QueueServiceClient actual, string accountName, Uri serviceUrl)
    {
        Assert.AreEqual(accountName, actual?.AccountName);
        Assert.AreEqual(serviceUrl, actual?.Uri);
    }
}
