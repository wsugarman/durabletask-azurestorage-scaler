// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.Accounts;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Accounts;

[TestClass]
public class CloudEndpointsExtensionsTest
{
    [TestMethod]
    public void GetStorageServiceUri()
    {
        // Exceptions
        _ = Assert.ThrowsException<ArgumentNullException>(() => CloudEndpointsExtensions.GetStorageServiceUri(null!, "foo", AzureStorageService.Blob));
        _ = Assert.ThrowsException<ArgumentNullException>(() => CloudEndpoints.Public.GetStorageServiceUri(null!, AzureStorageService.Blob));
        _ = Assert.ThrowsException<ArgumentException>(() => CloudEndpoints.Public.GetStorageServiceUri("", AzureStorageService.Blob));
        _ = Assert.ThrowsException<ArgumentOutOfRangeException>(() => CloudEndpoints.Public.GetStorageServiceUri("foo", (AzureStorageService)42));

        // Successful test cases
        Assert.AreEqual(new Uri("https://foo.blob.core.windows.net", UriKind.Absolute), CloudEndpoints.Public.GetStorageServiceUri("foo", AzureStorageService.Blob));
        Assert.AreEqual(new Uri("https://bar.queue.core.chinacloudapi.cn", UriKind.Absolute), CloudEndpoints.China.GetStorageServiceUri("bar", AzureStorageService.Queue));
        Assert.AreEqual(new Uri("https://baz.table.core.cloudapi.de", UriKind.Absolute), CloudEndpoints.Germany.GetStorageServiceUri("baz", AzureStorageService.Table));
        Assert.AreEqual(new Uri("https://test.file.core.usgovcloudapi.net", UriKind.Absolute), CloudEndpoints.USGovernment.GetStorageServiceUri("test", AzureStorageService.File));
    }
}
