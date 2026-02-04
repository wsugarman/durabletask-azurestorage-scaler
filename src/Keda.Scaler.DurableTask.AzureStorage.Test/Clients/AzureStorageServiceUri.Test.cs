// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.Clients;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Clients;

[TestClass]
public class AzureStorageServiceUriTest
{
    [TestMethod]
    public void GivenNullAccountName_WhenAzureStorageServiceUri_ThenThrowArgumentNullException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => AzureStorageServiceUri.Create(null!, AzureStorageService.Blob, AzureStorageServiceUri.PublicSuffix));

    [TestMethod]
    [DataRow("")]
    [DataRow("    \t\r\n")]
    public void GivenEmptyOrWhiteSpaceAccountName_WhenAzureStorageServiceUri_ThenThrowArgumentException(string accountName)
        => Assert.ThrowsExactly<ArgumentException>(() => AzureStorageServiceUri.Create(accountName, AzureStorageService.Blob, AzureStorageServiceUri.PublicSuffix));

    [TestMethod]
    public void GivenNullEndpointSuffix_WhenAzureStorageServiceUri_ThenThrowArgumentNullException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => AzureStorageServiceUri.Create("unittest", AzureStorageService.Blob, null!));

    [TestMethod]
    [DataRow("")]
    [DataRow("    \t\r\n")]
    public void GivenEmptyOrWhiteSpaceEndpointSuffix_WhenAzureStorageServiceUri_ThenThrowArgumentException(string endpointSuffix)
        => Assert.ThrowsExactly<ArgumentException>(() => AzureStorageServiceUri.Create("unittest", AzureStorageService.Blob, endpointSuffix));

    [TestMethod]
    public void GivenUnknownService_WhenGettingStorageServiceUri_ThenThrowArgumentOutOfRangeException()
        => Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => AzureStorageServiceUri.Create("unittest", (AzureStorageService)42, AzureStorageServiceUri.PublicSuffix));

    [TestMethod]
    [DataRow("https://foo.blob.core.windows.net", "foo", AzureStorageService.Blob, AzureStorageServiceUri.PublicSuffix)]
    [DataRow("https://bar.queue.core.chinacloudapi.cn", "bar", AzureStorageService.Queue, AzureStorageServiceUri.ChinaSuffix)]
    [DataRow("https://baz.table.core.usgovcloudapi.net", "baz", AzureStorageService.Table, AzureStorageServiceUri.USGovernmentSuffix)]
    public void GivenStorageAccount_WhenGettingStorageServiceUri_ThenReturnExpectedValue(string expected, string accountName, AzureStorageService service, string endpointSuffix)
        => Assert.AreEqual(new Uri(expected, UriKind.Absolute), AzureStorageServiceUri.Create(accountName, service, endpointSuffix));
}
