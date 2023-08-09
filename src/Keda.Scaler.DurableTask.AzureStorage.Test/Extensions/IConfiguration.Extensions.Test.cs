// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Google.Protobuf.Collections;
using Keda.Scaler.DurableTask.AzureStorage.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Extensions;

[TestClass]
public class IConfigurationExtensionsTest
{
    [TestMethod]
    public void GetOrDefault()
    {
        // Exception
        Assert.ThrowsException<ArgumentNullException>(() => IConfigurationExtensions.GetOrDefault<ScalerMetadata>(null!));

        ScalerMetadata actual;

        // Configuration contains some members
        MapField<string, string> map = new MapField<string, string>
            {
                { nameof(ScalerMetadata.AccountName), "unittest" },
                { nameof(ScalerMetadata.UseManagedIdentity), "true" },
            };

        actual = map.ToConfiguration().GetOrDefault<ScalerMetadata>();
        Assert.IsNotNull(actual);
        Assert.AreEqual("unittest", actual.AccountName);
        Assert.IsTrue(actual.UseManagedIdentity);

        // No members
        // (Get<T> would return null)
        IConfiguration empty = new MapField<string, string>().ToConfiguration();

        actual = empty.GetOrDefault<ScalerMetadata>();
        Assert.IsNotNull(actual);
        Assert.IsNull(empty.Get<ScalerMetadata>());
    }
}
