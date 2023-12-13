// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.Accounts;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Accounts;

public class CloudEndpointsExtensionsTest
{
    [Fact]
    public void GivenNullEndpoints_WhenGettingStorageServiceUri_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => CloudEndpointsExtensions.GetStorageServiceUri(null!, "foo", AzureStorageService.Blob));

    [Fact]
    public void GivenNullAccount_WhenGettingStorageServiceUri_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => AzureCloudEndpoints.Public.GetStorageServiceUri(null!, AzureStorageService.Blob));

    [Theory]
    [InlineData("")]
    [InlineData("    \t\r\n")]

    public void GivenEmptyOrWhiteSpaceAccount_WhenGettingStorageServiceUri_ThenThrowArgumentException(string accountName)
        => Assert.Throws<ArgumentException>(() => AzureCloudEndpoints.Public.GetStorageServiceUri(accountName, AzureStorageService.Table));

    [Fact]
    public void GivenUnknownService_WhenGettingStorageServiceUri_ThenThrowArgumentOutOfRangeException()
        => Assert.Throws<ArgumentOutOfRangeException>(() => AzureCloudEndpoints.Public.GetStorageServiceUri("foo", (AzureStorageService)42));

    [Theory]
    [InlineData("https://foo.blob.core.windows.net", CloudEnvironment.AzurePublicCloud, "foo", AzureStorageService.Blob)]
    [InlineData("https://bar.queue.core.chinacloudapi.cn", CloudEnvironment.AzureChinaCloud, "bar", AzureStorageService.Queue)]
    [InlineData("https://baz.table.core.cloudapi.de", CloudEnvironment.AzureGermanCloud, "baz", AzureStorageService.Table)]
    [InlineData("https://test.file.core.usgovcloudapi.net", CloudEnvironment.AzureUSGovernmentCloud, "test", AzureStorageService.File)]
    public void GivenStorageAccount_WhenGettingStorageServiceUri_ThenReturnExpectedValue(string expected, CloudEnvironment cloud, string accountName, AzureStorageService service)
        => Assert.Equal(new Uri(expected, UriKind.Absolute), AzureCloudEndpoints.ForEnvironment(cloud).GetStorageServiceUri(accountName, service));
}
