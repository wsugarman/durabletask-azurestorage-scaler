// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.Accounts;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Accounts;

public abstract class AzureStorageAccountClientFactoryTest<TClient>
{
    [TestMethod]
    public void GetServiceClient()
    {
        TClient actual;
        IStorageAccountClientFactory<TClient> factory = GetFactory();

        // Exceptions
        Assert.ThrowsException<ArgumentNullException>(() => factory.GetServiceClient(null!));
        Assert.ThrowsException<ArgumentException>(() => factory.GetServiceClient(new AzureStorageAccountInfo { AccountName = null, ConnectionString = null }));
        Assert.ThrowsException<ArgumentException>(() => factory.GetServiceClient(new AzureStorageAccountInfo { AccountName = "foo", Cloud = null }));

        // Connection String
        actual = factory.GetServiceClient(new AzureStorageAccountInfo { ConnectionString = "UseDevelopmentStorage=true" });
        ValidateEmulator(actual);

        // Service URI
        actual = factory.GetServiceClient(new AzureStorageAccountInfo { AccountName = "test", Cloud = CloudEndpoints.Public });
        ValidateAccountName(actual, "test", CloudEndpoints.Public);

        // Managed Identity
        actual = factory.GetServiceClient(new AzureStorageAccountInfo { AccountName = "test", Cloud = CloudEndpoints.Public, Credential = Credential.ManagedIdentity });
        ValidateAccountName(actual, "test", CloudEndpoints.Public); // TODO: Better indication managed identity was successfully used than code coverage
    }

    // Note: We use a method here to avoid exposing the internal implementation classes
    protected abstract IStorageAccountClientFactory<TClient> GetFactory();

    protected abstract void ValidateAccountName(TClient actual, string accountName, CloudEndpoints cloud);

    protected abstract void ValidateEmulator(TClient actual);
}
