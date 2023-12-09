// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Reflection;
using Azure.Core;
using Azure.Core.Pipeline;
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
    public void GivenEmptyAccountInfo_WhenGettingServiceClient_ThenThrowArgumentException(string? accountName, string? connectionString)
    {
        AzureStorageAccountInfo info = new() { AccountName = accountName, ConnectionString = connectionString, Cloud = CloudEndpoints.Public };
        IStorageAccountClientFactory<TClient> factory = GetFactory();
        _ = Assert.Throws<ArgumentException>(() => factory.GetServiceClient(info));
    }

    [Fact]
    public void GivenMissingCloudInfoForAccount_WhenGettingServiceClient_ThenThrowArgumentException()
    {
        AzureStorageAccountInfo info = new() { ConnectionString = null, Cloud = null };
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
        TClient actual = factory.GetServiceClient(new AzureStorageAccountInfo { AccountName = "test", Cloud = CloudEndpoints.Public });
        ValidateEmulator(actual);
    }

    [Fact]
    public void GivenServiceUriWithManagedIdentity_WhenGettingServiceClient_ThenReturnValidClient()
    {
        IStorageAccountClientFactory<TClient> factory = GetFactory();
        TClient actual = factory.GetServiceClient(new AzureStorageAccountInfo { AccountName = "test", Cloud = CloudEndpoints.Public });
        ValidateAccountName(actual, "test", CloudEndpoints.Public);
        AssertTokenCredential<ManagedIdentityCredential>(actual);
    }

    // Note: We use a method here to avoid exposing the internal implementation classes
    protected abstract IStorageAccountClientFactory<TClient> GetFactory();

    protected abstract void ValidateAccountName(TClient actual, string accountName, CloudEndpoints cloud);

    protected abstract void ValidateEmulator(TClient actual);

    private static void AssertTokenCredential<T>(TClient client) where T : TokenCredential
    {
        BearerTokenAuthenticationPolicy? authenticationPolicy = typeof(TClient)
            .GetProperty("AuthenticationPolicy", BindingFlags.NonPublic | BindingFlags.Instance)?
            .GetValue(client) as BearerTokenAuthenticationPolicy;

        Assert.NotNull(authenticationPolicy);
        object? accessTokenCache = typeof(BearerTokenAuthenticationPolicy)
            .GetField("_accessTokenCache", BindingFlags.NonPublic | BindingFlags.Instance)?
            .GetValue(authenticationPolicy);

        Assert.NotNull(accessTokenCache);
        Type? accessTokenCacheType = typeof(BearerTokenAuthenticationPolicy).GetNestedType("AccessTokenCache", BindingFlags.NonPublic);
        TokenCredential? tokenCredential = accessTokenCacheType?
            .GetField("_credential", BindingFlags.NonPublic | BindingFlags.Instance)?
            .GetValue(accessTokenCache) as TokenCredential;

        Assert.NotNull(accessTokenCache);
        _ = Assert.IsType<T>(tokenCredential);
    }
}
