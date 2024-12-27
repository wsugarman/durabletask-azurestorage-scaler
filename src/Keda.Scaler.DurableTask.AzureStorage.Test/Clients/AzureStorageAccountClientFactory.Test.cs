// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Reflection;
using Azure.Core;
using Azure.Identity;
using Keda.Scaler.DurableTask.AzureStorage.Clients;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Clients;

public abstract class AzureStorageAccountClientFactoryTest<TClient>
{
    [Fact]
    public void GivenNullAccountInfo_WhenGettingServiceClient_ThenThrowArgumentNullException()
    {
        AzureStorageAccountClientFactory<TClient> factory = GetFactory();
        _ = Assert.Throws<ArgumentNullException>(() => factory.GetServiceClient(null!));
    }

    [Fact]
    public void GivenConnectionString_WhenGettingServiceClient_ThenReturnValidClient()
    {
        AzureStorageAccountClientFactory<TClient> factory = GetFactory();
        TClient actual = factory.GetServiceClient(new AzureStorageAccountOptions { ConnectionString = "UseDevelopmentStorage=true" });
        ValidateEmulator(actual);
    }

    [Fact]
    public void GivenServiceUri_WhenGettingServiceClient_ThenReturnValidClient()
    {
        AzureStorageAccountClientFactory<TClient> factory = GetFactory();
        AzureStorageAccountOptions storageAccountInfo = new()
        {
            AccountName = "test",
            ConnectionString = null,
            EndpointSuffix = AzureStorageServiceUri.PublicSuffix,
            TokenCredential = new WorkloadIdentityCredential(
                new WorkloadIdentityCredentialOptions
                {
                    ClientId = Guid.NewGuid().ToString(),
                    TenantId = Guid.NewGuid().ToString(),
                    TokenFilePath = "/token.txt",
                }),
        };

        TClient actual = factory.GetServiceClient(storageAccountInfo);
        ValidateAccountName(actual, "test", AzureStorageServiceUri.PublicSuffix);
        AssertTokenCredential<WorkloadIdentityCredential>(actual);
    }

    // Note: We use a method here to avoid exposing the internal implementation classes
    protected abstract AzureStorageAccountClientFactory<TClient> GetFactory();

    protected abstract void ValidateAccountName(TClient actual, string accountName, string endpointSuffix);

    protected abstract void ValidateEmulator(TClient actual);

    protected virtual void AssertTokenCredential<T>(TClient client) where T : TokenCredential
    {
        object? configuration = typeof(TClient)
            .GetProperty("ClientConfiguration", BindingFlags.NonPublic | BindingFlags.Instance)?
            .GetValue(client);

        Assert.NotNull(configuration);
        TokenCredential? tokenCredential = typeof(TClient).Assembly
            .DefinedTypes
            .Single(x => x.FullName == "Azure.Storage.Shared.StorageClientConfiguration")
            .GetProperty("TokenCredential", BindingFlags.Public | BindingFlags.Instance)?
            .GetValue(configuration) as TokenCredential;

        Assert.NotNull(tokenCredential);
        _ = Assert.IsType<T>(tokenCredential);
    }
}
