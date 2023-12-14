// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.Accounts;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Accounts;

public class ScalerMetadataExtensionsTest
{
    [Fact]
    public void GivenNullMetadata_WhenGettingAccountInfo_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => ScalerMetadataExtensions.GetAccountInfo(null!));

    [Fact]
    public void GivenConnectionStringScalerMetadata_WhenGettingAccountInfo_ThenPassthroughData()
    {
        ScalerMetadata metadata = new() { Connection = "UseDevelopmentStorage=true" };
        AzureStorageAccountInfo actual = metadata.GetAccountInfo();
        Assert.Equal("UseDevelopmentStorage=true", actual.ConnectionString);
    }

    [Fact]
    public void GivenUnknownCloud_WhenGettingAccountInfo_ThenReturnNull()
    {
        ScalerMetadata metadata = new() { Cloud = "foo" };

        AzureStorageAccountInfo actual = metadata.GetAccountInfo();
        Assert.Null(actual.Cloud);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(CloudEnvironment.AzurePublicCloud)]
    [InlineData(CloudEnvironment.AzureUSGovernmentCloud)]
    [InlineData(CloudEnvironment.AzureGermanCloud)]
    [InlineData(CloudEnvironment.AzureChinaCloud)]
    public void GivenAccountScalerMetadata_WhenGettingAccountInfo_ThenPassthroughData(CloudEnvironment? cloud)
    {
        ScalerMetadata metadata = new()
        {
            AccountName = "foo",
            ClientId = Guid.NewGuid().ToString(),
            Cloud = cloud?.ToString("G"),
        };

        AzureStorageAccountInfo actual = metadata.GetAccountInfo();

        Assert.Equal(metadata.AccountName, actual.AccountName);
        Assert.Equal(metadata.ClientId, actual.ClientId);
        Assert.Same(AzureCloudEndpoints.ForEnvironment(cloud.GetValueOrDefault(CloudEnvironment.AzurePublicCloud)), actual.Cloud);
        Assert.Null(actual.Credential);
    }

    [Fact]
    public void GivenManagedIdentityScalerMetadata_WhenGettingAccountInfo_ThenPopulateCredential()
    {
        ScalerMetadata metadata = new()
        {
            AccountName = "foo",
            ClientId = Guid.NewGuid().ToString(),
            UseManagedIdentity = true,
        };

        AzureStorageAccountInfo actual = metadata.GetAccountInfo();

        Assert.Equal(metadata.AccountName, actual.AccountName);
        Assert.Equal(metadata.ClientId, actual.ClientId);
        Assert.Same(AzureCloudEndpoints.Public, actual.Cloud);
        Assert.Equal(Credential.ManagedIdentity, actual.Credential);
    }

    [Fact]
    public void GivenPrivateCloudScalerMetadata_WhenGettingAccountInfo_ThenCreateCustomCloudEndpoints()
    {
        ScalerMetadata metadata = new()
        {
            AccountName = "foo",
            ClientId = Guid.NewGuid().ToString(),
            Cloud = CloudEnvironment.Private.ToString("G"),
            ActiveDirectoryEndpoint = new Uri("https://unit-test.authority", UriKind.Absolute),
            EndpointSuffix = "storage.unit-test",
        };

        AzureStorageAccountInfo actual = metadata.GetAccountInfo();

        Assert.Equal(metadata.AccountName, actual.AccountName);
        Assert.Equal(metadata.ClientId, actual.ClientId);
        Assert.Equal(metadata.ActiveDirectoryEndpoint, actual.Cloud!.AuthorityHost);
        Assert.Equal(metadata.EndpointSuffix, actual.Cloud.StorageSuffix);
    }
}
