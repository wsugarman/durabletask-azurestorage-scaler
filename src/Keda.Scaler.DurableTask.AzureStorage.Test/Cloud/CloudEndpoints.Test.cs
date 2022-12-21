// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Azure.Identity;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Keda.Scaler.DurableTask.AzureStorage.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Cloud;

[TestClass]
public class CloudEndpointsTest
{
    [TestMethod]
    public void Public()
    {
        AssertEndpoints(AzureAuthorityHosts.AzurePublicCloud, "core.windows.net", CloudEndpoints.Public);
        AssertEndpoints(AzureAuthorityHosts.AzurePublicCloud, "core.windows.net", new CloudEndpoints());
    }

    [TestMethod]
    public void USGovernment()
        => AssertEndpoints(AzureAuthorityHosts.AzureGovernment, "core.usgovcloudapi.net", CloudEndpoints.USGovernment);

    [TestMethod]
    public void China()
        => AssertEndpoints(AzureAuthorityHosts.AzureChina, "core.chinacloudapi.cn", CloudEndpoints.China);

    [TestMethod]
    public void Germany()
        => AssertEndpoints(AzureAuthorityHosts.AzureGermany, "core.cloudapi.de", CloudEndpoints.Germany);

    [TestMethod]
    public void ForEnvironment()
    {
        Assert.AreSame(CloudEndpoints.Public, CloudEndpoints.ForEnvironment(CloudEnvironment.AzurePublicCloud));
        Assert.AreSame(CloudEndpoints.USGovernment, CloudEndpoints.ForEnvironment(CloudEnvironment.AzureUSGovernmentCloud));
        Assert.AreSame(CloudEndpoints.China, CloudEndpoints.ForEnvironment(CloudEnvironment.AzureChinaCloud));
        Assert.AreSame(CloudEndpoints.Germany, CloudEndpoints.ForEnvironment(CloudEnvironment.AzureGermanCloud));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => CloudEndpoints.ForEnvironment((CloudEnvironment)42));
    }

    private static void AssertEndpoints(Uri authorityHost, string storageSuffix, CloudEndpoints actual)
    {
        Assert.AreEqual(authorityHost, actual.AuthorityHost);
        Assert.AreEqual(storageSuffix, actual.StorageSuffix);
    }
}
