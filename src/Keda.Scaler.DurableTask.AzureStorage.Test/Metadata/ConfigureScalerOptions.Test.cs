// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Google.Protobuf.Collections;
using Keda.Scaler.DurableTask.AzureStorage.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Metadata;

[TestClass]
public class ConfigureScalerOptionsTest
{
    private readonly IScalerMetadataAccessor _metadataAccessor = Substitute.For<IScalerMetadataAccessor>();
    private readonly ConfigureScalerOptions _configure;

    public ConfigureScalerOptionsTest()
        => _configure = new(_metadataAccessor);

    [TestMethod]
    public void GivenNullScalerMetadataAccessor_WhenCreatingConfigure_ThenThrowArgumentNullException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => new ConfigureScalerOptions(null!));

    [TestMethod]
    public void GivenMissingScalerMetadata_WhenConfiguringOptions_ThenThrowInvalidOperationException()
    {
        _ = _metadataAccessor.ScalerMetadata.Returns(default(IReadOnlyDictionary<string, string?>));
        _ = Assert.ThrowsExactly<InvalidOperationException>(() => _configure.Configure(new ScalerOptions()));
    }

    [TestMethod]
    public void GivenScalerMetadata_WhenConfiguringOptions_ThenParseFields()
    {
        MapField<string, string?> metadata = new()
        {
            { nameof(ScalerOptions.AccountName), "AccountName" },
            { nameof(ScalerOptions.ClientId), "ClientId" },
            { nameof(ScalerOptions.Cloud), "Cloud" },
            { nameof(ScalerOptions.Connection), "Connection" },
            { nameof(ScalerOptions.ConnectionFromEnv), "ConnectionFromEnv" },
            { "endpointsuffix", "EndpointSuffix" },
            { "ENTRAENDPOINT", "https://unit.test.login/" },
            { nameof(ScalerOptions.MaxActivitiesPerWorker), "1" },
            { nameof(ScalerOptions.MaxOrchestrationsPerWorker), "2" },
            { nameof(ScalerOptions.TaskHubName), "TaskHubName" },
            { nameof(ScalerOptions.UseManagedIdentity), "true" },
            { nameof(ScalerOptions.UseTablePartitionManagement), "false" },
        };
        _ = _metadataAccessor.ScalerMetadata.Returns(metadata);

        ScalerOptions options = new();
        _configure.Configure(options);

        Assert.AreEqual("AccountName", options.AccountName);
        Assert.AreEqual("ClientId", options.ClientId);
        Assert.AreEqual("Cloud", options.Cloud);
        Assert.AreEqual("Connection", options.Connection);
        Assert.AreEqual("ConnectionFromEnv", options.ConnectionFromEnv);
        Assert.AreEqual("EndpointSuffix", options.EndpointSuffix);
        Assert.AreEqual("https://unit.test.login/", options.EntraEndpoint?.AbsoluteUri);
        Assert.AreEqual(1, options.MaxActivitiesPerWorker);
        Assert.AreEqual(2, options.MaxOrchestrationsPerWorker);
        Assert.AreEqual("TaskHubName", options.TaskHubName);
        Assert.IsTrue(options.UseManagedIdentity);
        Assert.IsFalse(options.UseTablePartitionManagement);
    }
}
