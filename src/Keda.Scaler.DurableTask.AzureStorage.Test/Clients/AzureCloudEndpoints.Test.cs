// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Azure.Identity;
using Keda.Scaler.DurableTask.AzureStorage.Clients;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Clients;

[TestClass]
public class AzureCloudEndpointsTest
{
    [TestMethod]
    public void GivenPublicCloud_WhenGettingEndpoints_ThenHaveExpectedValues()
        => AssertEndpoints(AzureAuthorityHosts.AzurePublicCloud, "core.windows.net", AzureCloudEndpoints.Public);

    [TestMethod]
    public void GivenUSGovernmentCloud_WhenGettingEndpoints_ThenHaveExpectedValues()
        => AssertEndpoints(AzureAuthorityHosts.AzureGovernment, "core.usgovcloudapi.net", AzureCloudEndpoints.USGovernment);

    [TestMethod]
    public void GivenChinaCloud_WhenGettingEndpoints_ThenHaveExpectedValues()
        => AssertEndpoints(AzureAuthorityHosts.AzureChina, "core.chinacloudapi.cn", AzureCloudEndpoints.China);

    [TestMethod]
    public void GivenNullAuthority_WhenCreatingCloudEndpoints_ThenThrowArgumentNullException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => new AzureCloudEndpoints(null!, "suffix"));

    [TestMethod]
    public void GivenNullStorageSuffix_WhenCreatingCloudEndpoints_ThenThrowArgumentNullException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => new AzureCloudEndpoints(new Uri("https://test"), null!));

    [TestMethod]
    [DataRow("")]
    [DataRow("\r\n  ")]
    public void GivenEmptyOrWhiteSpaceStorageSuffix_WhenCreatingCloudEndpoints_ThenThrowArgumentException(string suffix)
        => Assert.ThrowsExactly<ArgumentException>(() => new AzureCloudEndpoints(new Uri("https://test"), suffix));

    [TestMethod]
    [DataRow(CloudEnvironment.Unknown)]
    [DataRow((CloudEnvironment)12)]
    public void GivenUnknownCloud_WhenGettingEndpointsForEnvironment_ThenThrowArgumentOutOfRangeException(CloudEnvironment environment)
        => Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => AzureCloudEndpoints.ForEnvironment(environment));

    [TestMethod]
    public void GivenPublicCloud_WhenGettingEndpointsForEnvironment_ThenReturnExpectedInstance()
        => Assert.AreSame(AzureCloudEndpoints.Public, AzureCloudEndpoints.ForEnvironment(CloudEnvironment.AzurePublicCloud));

    [TestMethod]
    public void GivenUSGovernmentCloud_WhenGettingEndpointsForEnvironment_ThenReturnExpectedInstance()
        => Assert.AreSame(AzureCloudEndpoints.USGovernment, AzureCloudEndpoints.ForEnvironment(CloudEnvironment.AzureUSGovernmentCloud));

    [TestMethod]
    public void GivenChinaCloud_WhenGettingEndpointsForEnvironment_ThenReturnExpectedInstance()
        => Assert.AreSame(AzureCloudEndpoints.China, AzureCloudEndpoints.ForEnvironment(CloudEnvironment.AzureChinaCloud));

    [TestMethod]
    [DataRow(CloudEnvironment.AzurePublicCloud, null)]
    [DataRow(CloudEnvironment.AzurePublicCloud, nameof(CloudEnvironment.AzurePublicCloud))]
    [DataRow(CloudEnvironment.AzurePublicCloud, "azurepubliccloud")]
    [DataRow(CloudEnvironment.AzureUSGovernmentCloud, nameof(CloudEnvironment.AzureUSGovernmentCloud))]
    [DataRow(CloudEnvironment.AzureUSGovernmentCloud, "AZUREUSGOVERNMENTCLOUD")]
    [DataRow(CloudEnvironment.AzureChinaCloud, nameof(CloudEnvironment.AzureChinaCloud))]
    [DataRow(CloudEnvironment.AzureChinaCloud, "azureCHINAcloud")]
    [DataRow(CloudEnvironment.Private, nameof(CloudEnvironment.Private))]
    [DataRow(CloudEnvironment.Private, "priVATE")]
    public void GivenEnvironmentName_WhenParsingEnvironment_ThenReturnTrue(CloudEnvironment expected, string? cloud)
    {
        Assert.IsTrue(AzureCloudEndpoints.TryParseEnvironment(cloud, out CloudEnvironment actual));
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("AzureUnknownCloud")]
    public void GivenUnknownEnvironmentName_WhenParsingEnvironment_ThenReturnFalse(string cloud)
    {
        Assert.IsFalse(AzureCloudEndpoints.TryParseEnvironment(cloud, out CloudEnvironment actual));
        Assert.AreEqual(CloudEnvironment.Unknown, actual);
    }

    private static void AssertEndpoints(Uri authorityHost, string storageSuffix, AzureCloudEndpoints actual)
    {
        Assert.AreEqual(authorityHost, actual.AuthorityHost);
        Assert.AreEqual(storageSuffix, actual.StorageSuffix);
    }
}
