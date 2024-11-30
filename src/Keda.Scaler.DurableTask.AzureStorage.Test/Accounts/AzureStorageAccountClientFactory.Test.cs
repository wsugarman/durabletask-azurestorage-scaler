// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Reflection;
using Azure.Core;
using Azure.Identity;
using Keda.Scaler.DurableTask.AzureStorage.Accounts;
using Keda.Scaler.DurableTask.AzureStorage.Clients;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Microsoft.Identity.Client;
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
        AzureStorageAccountOptions info = new() { AccountName = accountName, ConnectionString = connectionString, Cloud = AzureCloudEndpoints.Public };
        IStorageAccountClientFactory<TClient> factory = GetFactory();
        _ = Assert.Throws<ArgumentException>(() => factory.GetServiceClient(info));
    }

    [Fact]
    public void GivenMissingCloudInfoForAccount_WhenGettingServiceClient_ThenThrowArgumentException()
    {
        AzureStorageAccountOptions info = new() { AccountName = "account", ConnectionString = null, Cloud = null };
        IStorageAccountClientFactory<TClient> factory = GetFactory();
        _ = Assert.Throws<ArgumentException>(() => factory.GetServiceClient(info));
    }

    [Fact]
    public void GivenConnectionString_WhenGettingServiceClient_ThenReturnValidClient()
    {
        IStorageAccountClientFactory<TClient> factory = GetFactory();
        TClient actual = factory.GetServiceClient(new AzureStorageAccountOptions { ConnectionString = "UseDevelopmentStorage=true" });
        ValidateEmulator(actual);
    }

    [Fact]
    public void GivenServiceUri_WhenGettingServiceClient_ThenReturnValidClient()
    {
        IStorageAccountClientFactory<TClient> factory = GetFactory();
        TClient actual = factory.GetServiceClient(new AzureStorageAccountOptions { AccountName = "test", Cloud = AzureCloudEndpoints.USGovernment });
        ValidateAccountName(actual, "test", AzureCloudEndpoints.USGovernment);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("123456789")]
    [Obsolete("Will be replaced by Workload Identity.")]
    public void GivenServiceUriWithManagedIdentity_WhenGettingServiceClient_ThenReturnValidClient(string? clientId)
    {
        IStorageAccountClientFactory<TClient> factory = GetFactory();
        AzureStorageAccountOptions storageAccountInfo = new()
        {
            AccountName = "test",
            Cloud = AzureCloudEndpoints.Public,
            Credential = Credentials.ManagedIdentity,
            ClientId = clientId,
        };

        TClient actual = factory.GetServiceClient(storageAccountInfo);
        ValidateAccountName(actual, "test", AzureCloudEndpoints.Public);

        ManagedIdentityCredential actualCredential = AssertTokenCredential<ManagedIdentityCredential>(actual);
        AssertClientId(actualCredential, clientId);
    }

    [Theory]
    [InlineData("123", "123", null)]
    [InlineData("ABC", "123", "ABC")]
    public void GivenServiceUriWithWorkloadIdentity_WhenGettingServiceClient_ThenReturnValidClient(string expected, string? env, string? clientId)
    {
        using IDisposable tenant = TestEnvironment.SetVariable("AZURE_TENANT_ID", Guid.NewGuid().ToString());
        using IDisposable client = TestEnvironment.SetVariable("AZURE_CLIENT_ID", env);
        using IDisposable tokenFile = TestEnvironment.SetVariable("AZURE_FEDERATED_TOKEN_FILE", "/token.txt");

        IStorageAccountClientFactory<TClient> factory = GetFactory();
        AzureStorageAccountOptions storageAccountInfo = new()
        {
            AccountName = "test",
            Cloud = AzureCloudEndpoints.Public,
            Credential = Credentials.WorkloadIdentity,
            ClientId = clientId,
        };

        TClient actual = factory.GetServiceClient(storageAccountInfo);
        ValidateAccountName(actual, "test", AzureCloudEndpoints.Public);

        WorkloadIdentityCredential actualCredential = AssertTokenCredential<WorkloadIdentityCredential>(actual);
        AssertClientId(actualCredential, expected);
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
            .GetProperty("EntraClientId", BindingFlags.NonPublic | BindingFlags.Instance)?
            .GetValue(client) as string;

        Assert.Equal(expected, actual);
    }

    private static void AssertClientId(WorkloadIdentityCredential tokenCredential, string? expected)
    {
        object? client = typeof(WorkloadIdentityCredential)
            .GetProperty("Client", BindingFlags.NonPublic | BindingFlags.Instance)?
            .GetValue(tokenCredential);

        Assert.NotNull(client);
        string? actual = typeof(ManagedIdentityCredential).Assembly
            .DefinedTypes
            .Single(x => x.FullName == "Azure.Identity.MsalClientBase`1")
            .MakeGenericType(typeof(IConfidentialClientApplication))
            .GetProperty("EntraClientId", BindingFlags.NonPublic | BindingFlags.Instance)?
            .GetValue(client) as string;

        Assert.Equal(expected, actual);
    }
}
