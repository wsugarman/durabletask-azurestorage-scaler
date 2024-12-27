// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Google.Protobuf.Collections;
using Keda.Scaler.DurableTask.AzureStorage.Metadata;
using NSubstitute;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Metadata;

public class ConfigureScalerOptionsTest
{
    private readonly IScalerMetadataAccessor _metadataAccessor = Substitute.For<IScalerMetadataAccessor>();
    private readonly ConfigureScalerOptions _configure;

    public ConfigureScalerOptionsTest()
        => _configure = new(_metadataAccessor);

    [Fact]
    public void GivenNullScalerMetadataAccessor_WhenCreatingConfigure_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new ConfigureScalerOptions(null!));

    [Fact]
    public void GivenMissingScalerMetadata_WhenConfiguringOptions_ThenThrowInvalidOperationException()
    {
        _ = _metadataAccessor.ScalerMetadata.Returns(default(IReadOnlyDictionary<string, string?>));
        _ = Assert.Throws<InvalidOperationException>(() => _configure.Configure(new ScalerOptions()));
    }

    [Fact]
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

        Assert.Equal("AccountName", options.AccountName);
        Assert.Equal("ClientId", options.ClientId);
        Assert.Equal("Cloud", options.Cloud);
        Assert.Equal("Connection", options.Connection);
        Assert.Equal("ConnectionFromEnv", options.ConnectionFromEnv);
        Assert.Equal("EndpointSuffix", options.EndpointSuffix);
        Assert.Equal("https://unit.test.login/", options.EntraEndpoint?.AbsoluteUri);
        Assert.Equal(1, options.MaxActivitiesPerWorker);
        Assert.Equal(2, options.MaxOrchestrationsPerWorker);
        Assert.Equal("TaskHubName", options.TaskHubName);
        Assert.True(options.UseManagedIdentity);
        Assert.False(options.UseTablePartitionManagement);
    }
}
