// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Google.Protobuf.Collections;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Keda.Scaler.DurableTask.AzureStorage.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Extensions;

[TestClass]
public class MapFieldExtensionsTest
{
    [TestMethod]
    public void ToConfiguration()
    {
        Assert.ThrowsException<ArgumentNullException>(() => MapFieldExtensions.ToConfiguration(null!));

        // We already ensure that the parsing into a configuration works in MapFieldConfiguration.Test.cs,
        // so we'll demonstrate the usage inside of the KEDA scaler code below.
        MapField<string, string> raw = new MapField<string, string>
            {
                { nameof(ScalerMetadata.AccountName), "unittest" },
                { nameof(ScalerMetadata.Cloud), "AzureUSGovernmentCloud" }, // non-default
                { nameof(ScalerMetadata.Connection), "foo=bar;hello=world" },
                { nameof(ScalerMetadata.ConnectionFromEnv), "MY_CONNECTION_STRING" },
                { nameof(ScalerMetadata.MaxActivitiesPerWorker), "10" },
                { nameof(ScalerMetadata.MaxOrchestrationsPerWorker), "3" },
                { nameof(ScalerMetadata.TaskHubName), "MyTaskHub" },
                { nameof(ScalerMetadata.UseManagedIdentity), "true" },
            };

        ScalerMetadata? actual = raw.ToConfiguration().Get<ScalerMetadata>()!;
        Assert.IsNotNull(actual);
        Assert.AreEqual("unittest", actual.AccountName);
        Assert.AreEqual(nameof(CloudEnvironment.AzureUSGovernmentCloud), actual.Cloud);
        Assert.AreEqual("foo=bar;hello=world", actual.Connection);
        Assert.AreEqual("MY_CONNECTION_STRING", actual.ConnectionFromEnv);
        Assert.AreEqual(10, actual.MaxActivitiesPerWorker);
        Assert.AreEqual(3, actual.MaxOrchestrationsPerWorker);
        Assert.AreEqual("MyTaskHub", actual.TaskHubName);
        Assert.IsTrue(actual.UseManagedIdentity);
    }
}
