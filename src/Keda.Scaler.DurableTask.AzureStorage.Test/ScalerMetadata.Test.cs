// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Keda.Scaler.DurableTask.AzureStorage.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test;

[TestClass]
public class ScalerMetadataTest
{
    [TestMethod]
    public void CloudEnvironmentProperty()
    {
        Assert.AreEqual(CloudEnvironment.AzurePublicCloud, new ScalerMetadata { Cloud = null }.CloudEnvironment);
        Assert.AreEqual(CloudEnvironment.AzurePublicCloud, new ScalerMetadata { Cloud = nameof(CloudEnvironment.AzurePublicCloud) }.CloudEnvironment);
        Assert.AreEqual(CloudEnvironment.AzureUSGovernmentCloud, new ScalerMetadata { Cloud = nameof(CloudEnvironment.AzureUSGovernmentCloud) }.CloudEnvironment);
        Assert.AreEqual(CloudEnvironment.AzureChinaCloud, new ScalerMetadata { Cloud = nameof(CloudEnvironment.AzureChinaCloud) }.CloudEnvironment);
        Assert.AreEqual(CloudEnvironment.AzureGermanCloud, new ScalerMetadata { Cloud = nameof(CloudEnvironment.AzureGermanCloud) }.CloudEnvironment);
        Assert.AreEqual(CloudEnvironment.Unknown, new ScalerMetadata { Cloud = "foo" }.CloudEnvironment);
    }

    [TestMethod]
    public void ResolveConnectionString()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new ScalerMetadata().ResolveConnectionString(null!));

        MockEnvironment env = new MockEnvironment();
        env.SetEnvironmentVariable(ScalerMetadata.DefaultConnectionEnvironmentVariable, "one=1");
        env.SetEnvironmentVariable("MY_CONNECTION", "two=2");

        Assert.AreEqual("one=1", new ScalerMetadata().ResolveConnectionString(env));
        Assert.AreEqual("two=2", new ScalerMetadata { ConnectionFromEnv = "MY_CONNECTION" }.ResolveConnectionString(env));
        Assert.AreEqual("three=3", new ScalerMetadata { Connection = "three=3" }.ResolveConnectionString(env));
    }

    [TestMethod]
    public void Validate()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new ScalerMetadata().Validate(null!).ToList());

        MockEnvironment env = new MockEnvironment();
        env.SetEnvironmentVariable(ScalerMetadata.DefaultConnectionEnvironmentVariable, "UseDevelopmentStorage=true");

        IServiceProvider provider = new ServiceCollection()
            .AddSingleton<IProcessEnvironment>(env)
            .BuildServiceProvider();

        // Default value is valid
        AssertValidation(new ScalerMetadata(), provider, 0);

        // Negative MaxMessageLatencyMilliseconds
        AssertValidation(new ScalerMetadata { MaxMessageLatencyMilliseconds = -1 }, provider, 1);

        // MaxMessageLatencyMilliseconds too large
        AssertValidation(new ScalerMetadata { MaxMessageLatencyMilliseconds = 2000 }, provider, 1);

        // Invalid ScaleIncrement
        AssertValidation(new ScalerMetadata { ScaleIncrement = -1 }, provider, 1);

        // Null or white space TaskHubName
        AssertValidation(new ScalerMetadata { TaskHubName = null! }, provider, 1);
        AssertValidation(new ScalerMetadata { TaskHubName = "" }, provider, 1);
        AssertValidation(new ScalerMetadata { TaskHubName = "\t" }, provider, 1);

        // AAD + No Account
        AssertValidation(new ScalerMetadata { AccountName = null, UseManagedIdentity = true }, provider, 1);
        AssertValidation(new ScalerMetadata { AccountName = "", UseManagedIdentity = true }, provider, 1);
        AssertValidation(new ScalerMetadata { AccountName = "\t", UseManagedIdentity = true }, provider, 1);

        // AAD + Unknown Cloud
        AssertValidation(new ScalerMetadata { AccountName = "mytestaccount", Cloud = "foobar", UseManagedIdentity = true }, provider, 1);

        // AAD + Connection
        AssertValidation(new ScalerMetadata { AccountName = "mytestaccount", Connection = "UseDevelopmentStorage=true", UseManagedIdentity = true }, provider, 1);

        // AAD + ConnectionFromEnv
        AssertValidation(new ScalerMetadata { AccountName = "mytestaccount", ConnectionFromEnv = "MY_CONNECTION", UseManagedIdentity = true }, provider, 1);

        // No AAD + Account
        AssertValidation(new ScalerMetadata { AccountName = "mytestaccount", UseManagedIdentity = false }, provider, 1);

        // No AAD + Cloud
        AssertValidation(new ScalerMetadata { Cloud = "AzurePublicCloud", UseManagedIdentity = false }, provider, 1);

        // No AAD + Invalid Connection
        AssertValidation(new ScalerMetadata { Connection = "", UseManagedIdentity = false }, provider, 1);
        AssertValidation(new ScalerMetadata { Connection = "\t", UseManagedIdentity = false }, provider, 1);

        // No AAD + Invalid ConnectionFromEnv
        AssertValidation(new ScalerMetadata { ConnectionFromEnv = "", UseManagedIdentity = false }, provider, 1);
        AssertValidation(new ScalerMetadata { ConnectionFromEnv = "\t", UseManagedIdentity = false }, provider, 1);

        // No AAD + Cannot resolve connection string
        AssertValidation(new ScalerMetadata { ConnectionFromEnv = "MY_CONNECTION", UseManagedIdentity = false }, provider, 1);

        // No AAD + Cannot resolve default connection string
        env.SetEnvironmentVariable(ScalerMetadata.DefaultConnectionEnvironmentVariable, null);
        AssertValidation(new ScalerMetadata(), provider, 1);
    }

    private static void AssertValidation(ScalerMetadata metadata, IServiceProvider provider, int expectedErrors)
    {
        List<ValidationResult> errors = metadata.Validate(new ValidationContext(metadata, provider, null)).ToList();
        Assert.AreEqual(expectedErrors, errors.Count, errors.FirstOrDefault()?.ErrorMessage);
    }
}
