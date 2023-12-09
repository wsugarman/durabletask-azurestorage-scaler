// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Azure.Storage.Queues;
using Keda.Scaler.DurableTask.AzureStorage.Accounts;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Accounts;

public class QueueServiceClientFactoryTest : AzureStorageAccountClientFactoryTest<QueueServiceClient>
{
    protected override IStorageAccountClientFactory<QueueServiceClient> GetFactory()
        => new QueueServiceClientFactory();

    protected override void ValidateAccountName(QueueServiceClient actual, string accountName, CloudEndpoints cloud)
        => Validate(actual, accountName, cloud.GetStorageServiceUri(accountName, AzureStorageService.Queue));

    protected override void ValidateEmulator(QueueServiceClient actual)
        => Validate(actual, "devstoreaccount1", new Uri("http://127.0.0.1:10001/devstoreaccount1", UriKind.Absolute));

    private static void Validate(QueueServiceClient actual, string accountName, Uri serviceUrl)
    {
        Assert.Equal(accountName, actual?.AccountName);
        Assert.Equal(serviceUrl, actual?.Uri);
    }
}
