// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.Clients;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Clients;

public class AzureStorageServiceUriTest
{
    [Fact]
    public void GivenNullAccountName_WhenAzureStorageServiceUri_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => AzureStorageServiceUri.Create(null!, AzureStorageService.Blob, AzureStorageServiceUri.PublicSuffix));

    [Theory]
    [InlineData("")]
    [InlineData("    \t\r\n")]
    public void GivenEmptyOrWhiteSpaceAccountName_WhenAzureStorageServiceUri_ThenThrowArgumentException(string accountName)
        => Assert.Throws<ArgumentException>(() => AzureStorageServiceUri.Create(accountName, AzureStorageService.Blob, AzureStorageServiceUri.PublicSuffix));

    [Fact]
    public void GivenNullEndpointSuffix_WhenAzureStorageServiceUri_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => AzureStorageServiceUri.Create("unittest", AzureStorageService.Blob, null!));

    [Theory]
    [InlineData("")]
    [InlineData("    \t\r\n")]
    public void GivenEmptyOrWhiteSpaceEndpointSuffix_WhenAzureStorageServiceUri_ThenThrowArgumentException(string endpointSuffix)
        => Assert.Throws<ArgumentException>(() => AzureStorageServiceUri.Create("unittest", AzureStorageService.Blob, endpointSuffix));

    [Fact]
    public void GivenUnknownService_WhenGettingStorageServiceUri_ThenThrowArgumentOutOfRangeException()
        => Assert.Throws<ArgumentOutOfRangeException>(() => AzureStorageServiceUri.Create("unittest", (AzureStorageService)42, AzureStorageServiceUri.PublicSuffix));

    [Theory]
    [InlineData("https://foo.blob.core.windows.net", "foo", AzureStorageService.Blob, AzureStorageServiceUri.PublicSuffix)]
    [InlineData("https://bar.queue.core.chinacloudapi.cn", "bar", AzureStorageService.Queue, AzureStorageServiceUri.ChinaSuffix)]
    [InlineData("https://baz.table.core.usgovcloudapi.net", "baz", AzureStorageService.Table, AzureStorageServiceUri.USGovernmentSuffix)]
    public void GivenStorageAccount_WhenGettingStorageServiceUri_ThenReturnExpectedValue(string expected, string accountName, AzureStorageService service, string endpointSuffix)
        => Assert.Equal(new Uri(expected, UriKind.Absolute), AzureStorageServiceUri.Create(accountName, service, endpointSuffix));
}
