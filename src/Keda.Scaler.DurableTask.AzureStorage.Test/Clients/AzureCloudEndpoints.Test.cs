// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Azure.Identity;
using Keda.Scaler.DurableTask.AzureStorage.Clients;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Clients;

public class AzureCloudEndpointsTest
{
    [Fact]
    public void GivenPublicCloud_WhenGettingEndpoints_ThenHaveExpectedValues()
        => AssertEndpoints(AzureAuthorityHosts.AzurePublicCloud, "core.windows.net", AzureCloudEndpoints.Public);

    [Fact]
    public void GivenUSGovernmentCloud_WhenGettingEndpoints_ThenHaveExpectedValues()
        => AssertEndpoints(AzureAuthorityHosts.AzureGovernment, "core.usgovcloudapi.net", AzureCloudEndpoints.USGovernment);

    [Fact]
    public void GivenChinaCloud_WhenGettingEndpoints_ThenHaveExpectedValues()
        => AssertEndpoints(AzureAuthorityHosts.AzureChina, "core.chinacloudapi.cn", AzureCloudEndpoints.China);

    [Fact]
    public void GivenNullAuthority_WhenCreatingCloudEndpoints_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new AzureCloudEndpoints(null!, "suffix"));

    [Fact]
    public void GivenNullStorageSuffix_WhenCreatingCloudEndpoints_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new AzureCloudEndpoints(new Uri("https://test"), null!));

    [Theory]
    [InlineData("")]
    [InlineData("\r\n  ")]
    public void GivenEmptyOrWhiteSpaceStorageSuffix_WhenCreatingCloudEndpoints_ThenThrowArgumentException(string suffix)
        => Assert.Throws<ArgumentException>(() => new AzureCloudEndpoints(new Uri("https://test"), suffix));

    [Theory]
    [InlineData(CloudEnvironment.Unknown)]
    [InlineData((CloudEnvironment)12)]
    public void GivenUnknownCloud_WhenGettingEndpointsForEnvironment_ThenThrowArgumentOutOfRangeException(CloudEnvironment environment)
        => Assert.Throws<ArgumentOutOfRangeException>(() => AzureCloudEndpoints.ForEnvironment(environment));

    [Fact]
    public void GivenPublicCloud_WhenGettingEndpointsForEnvironment_ThenReturnExpectedInstance()
        => Assert.Same(AzureCloudEndpoints.Public, AzureCloudEndpoints.ForEnvironment(CloudEnvironment.AzurePublicCloud));

    [Fact]
    public void GivenUSGovernmentCloud_WhenGettingEndpointsForEnvironment_ThenReturnExpectedInstance()
        => Assert.Same(AzureCloudEndpoints.USGovernment, AzureCloudEndpoints.ForEnvironment(CloudEnvironment.AzureUSGovernmentCloud));

    [Fact]
    public void GivenChinaCloud_WhenGettingEndpointsForEnvironment_ThenReturnExpectedInstance()
        => Assert.Same(AzureCloudEndpoints.China, AzureCloudEndpoints.ForEnvironment(CloudEnvironment.AzureChinaCloud));

    [Theory]
    [InlineData(CloudEnvironment.AzurePublicCloud, null)]
    [InlineData(CloudEnvironment.AzurePublicCloud, nameof(CloudEnvironment.AzurePublicCloud))]
    [InlineData(CloudEnvironment.AzurePublicCloud, "azurepubliccloud")]
    [InlineData(CloudEnvironment.AzureUSGovernmentCloud, nameof(CloudEnvironment.AzureUSGovernmentCloud))]
    [InlineData(CloudEnvironment.AzureUSGovernmentCloud, "AZUREUSGOVERNMENTCLOUD")]
    [InlineData(CloudEnvironment.AzureChinaCloud, nameof(CloudEnvironment.AzureChinaCloud))]
    [InlineData(CloudEnvironment.AzureChinaCloud, "azureCHINAcloud")]
    [InlineData(CloudEnvironment.Private, nameof(CloudEnvironment.Private))]
    [InlineData(CloudEnvironment.Private, "priVATE")]
    public void GivenEnvironmentName_WhenParsingEnvironment_ThenReturnTrue(CloudEnvironment expected, string? cloud)
    {
        Assert.True(AzureCloudEndpoints.TryParseEnvironment(cloud, out CloudEnvironment actual));
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("AzureUnknownCloud")]
    public void GivenUnknownEnvironmentName_WhenParsingEnvironment_ThenReturnFalse(string cloud)
    {
        Assert.False(AzureCloudEndpoints.TryParseEnvironment(cloud, out CloudEnvironment actual));
        Assert.Equal(CloudEnvironment.Unknown, actual);
    }

    private static void AssertEndpoints(Uri authorityHost, string storageSuffix, AzureCloudEndpoints actual)
    {
        Assert.Equal(authorityHost, actual.AuthorityHost);
        Assert.Equal(storageSuffix, actual.StorageSuffix);
    }
}
