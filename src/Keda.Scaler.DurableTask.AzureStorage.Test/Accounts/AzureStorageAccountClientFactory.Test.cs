// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Reflection;
using Azure.Core;
using Azure.Identity;
using Keda.Scaler.DurableTask.AzureStorage.Accounts;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Accounts;

public abstract class AzureStorageAccountClientFactoryTest<TClient>
{
    [Fact]
    public void GivenNullAccountInfo_WhenGettingServiceClient_ThenThrowArgumentNullException()
    {
        IStorageAccountClientFactory<TClient> factory = GetFactory();
        _ = Assert.Throws<ArgumentNullException>(() => factory.GetServiceClient(null!));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("  ", "\t")]
    [InlineData("", null)]
    public void GivenEmptyOrWhiteSpaceAccountInfo_WhenGettingServiceClient_ThenThrowArgumentException(string? accountName, string? connectionString)
    {
        AzureStorageAccountInfo info = new() { AccountName = accountName, ConnectionString = connectionString, Cloud = AzureCloudEndpoints.Public };
        IStorageAccountClientFactory<TClient> factory = GetFactory();
        _ = Assert.Throws<ArgumentException>(() => factory.GetServiceClient(info));
    }

    [Fact]
    public void GivenMissingCloudInfoForAccount_WhenGettingServiceClient_ThenThrowArgumentException()
    {
        AzureStorageAccountInfo info = new() { AccountName = "account", ConnectionString = null, Cloud = null };
        IStorageAccountClientFactory<TClient> factory = GetFactory();
        _ = Assert.Throws<ArgumentException>(() => factory.GetServiceClient(info));
    }

    [Fact]
    public void GivenConnectionString_WhenGettingServiceClient_ThenReturnValidClient()
    {
        IStorageAccountClientFactory<TClient> factory = GetFactory();
        TClient actual = factory.GetServiceClient(new AzureStorageAccountInfo { ConnectionString = "UseDevelopmentStorage=true" });
        ValidateEmulator(actual);
    }

    [Fact]
    public void GivenServiceUri_WhenGettingServiceClient_ThenReturnValidClient()
    {
        IStorageAccountClientFactory<TClient> factory = GetFactory();
        TClient actual = factory.GetServiceClient(new AzureStorageAccountInfo { AccountName = "test", Cloud = AzureCloudEndpoints.Germany });
        ValidateAccountName(actual, "test", AzureCloudEndpoints.Germany);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("123456789")]
    public void GivenServiceUriWithManagedIdentity_WhenGettingServiceClient_ThenReturnValidClient(string? clientId)
    {
        IStorageAccountClientFactory<TClient> factory = GetFactory();
        AzureStorageAccountInfo storageAccountInfo = new()
        {
            AccountName = "test",
            Cloud = AzureCloudEndpoints.Public,
            Credential = Credential.ManagedIdentity,
            ClientId = clientId,
        };

        TClient actual = factory.GetServiceClient(storageAccountInfo);
        ValidateAccountName(actual, "test", AzureCloudEndpoints.Public);

        ManagedIdentityCredential actualCredential = AssertTokenCredential<ManagedIdentityCredential>(actual);
        AssertClientId(actualCredential, clientId);
    }

    // Note: We use a method here to avoid exposing the internal implementation classes
    protected abstract IStorageAccountClientFactory<TClient> GetFactory();

    protected abstract void ValidateAccountName(TClient actual, string accountName, AzureCloudEndpoints cloud);

    protected abstract void ValidateEmulator(TClient actual);

    private static T AssertTokenCredential<T>(TClient client) where T : TokenCredential
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
        return Assert.IsType<T>(tokenCredential);
    }

    private static void AssertClientId(ManagedIdentityCredential tokenCredential, string? expected)
    {
        object? client = typeof(ManagedIdentityCredential)
            .GetProperty("Client", BindingFlags.NonPublic | BindingFlags.Instance)?
            .GetValue(tokenCredential);

        Assert.NotNull(client);
        string? actual = typeof(ManagedIdentityCredential).Assembly
            .DefinedTypes
            .Single(x => x.FullName == "Azure.Identity.ManagedIdentityClient")
            .GetProperty("ClientId", BindingFlags.NonPublic | BindingFlags.Instance)?
            .GetValue(client) as string;

        Assert.Equal(expected, actual);
    }
}
