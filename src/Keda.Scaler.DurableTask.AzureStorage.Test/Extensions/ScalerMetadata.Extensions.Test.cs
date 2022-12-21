// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.Accounts;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Keda.Scaler.DurableTask.AzureStorage.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Extensions;

[TestClass]
public class ScalerMetadataExtensionsTest
{
    [TestMethod]
    public void GetAccountInfo()
    {
        ScalerMetadata metadata;
        AzureStorageAccountInfo actual;
        MockEnvironment env = new MockEnvironment();
        env.SetEnvironmentVariable("Connection", "UseDevelopmentStorage=true");

        // Errors
        Assert.ThrowsException<ArgumentNullException>(() => ScalerMetadataExtensions.GetAccountInfo(null!, env));
        Assert.ThrowsException<ArgumentNullException>(() => ScalerMetadataExtensions.GetAccountInfo(new ScalerMetadata(), null!));

        // No managed identity
        // Note: Typically you don't specify values for some of these members in combination,
        //       but in this test we will do so for the sake of simplicity
        metadata = new ScalerMetadata
        {
            AccountName = "test",
            ClientId = Guid.NewGuid().ToString(),
            Cloud = nameof(CloudEnvironment.AzurePublicCloud),
            ConnectionFromEnv = "Connection",
        };

        actual = metadata.GetAccountInfo(env);
        Assert.AreEqual("test", actual.AccountName);
        Assert.AreEqual(metadata.ClientId, actual.ClientId);
        Assert.AreEqual(CloudEnvironment.AzurePublicCloud, actual.CloudEnvironment);
        Assert.AreEqual("UseDevelopmentStorage=true", actual.ConnectionString);
        Assert.AreEqual(null, actual.Credential);

        // Use managed identity
        metadata = new ScalerMetadata
        {
            AccountName = "test2",
            ClientId = Guid.NewGuid().ToString(),
            Cloud = nameof(CloudEnvironment.AzureUSGovernmentCloud),
            ConnectionFromEnv = "Connection",
            UseManagedIdentity = true,
        };

        actual = metadata.GetAccountInfo(env);
        Assert.AreEqual("test2", actual.AccountName);
        Assert.AreEqual(metadata.ClientId, actual.ClientId);
        Assert.AreEqual(CloudEnvironment.AzureUSGovernmentCloud, actual.CloudEnvironment);
        Assert.AreEqual("UseDevelopmentStorage=true", actual.ConnectionString);
        Assert.AreEqual(Credential.ManagedIdentity, actual.Credential);
    }
}
