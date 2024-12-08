// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.TaskHubs;
using NSubstitute;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHubs;

public class ConfigureTaskHubOptionsTest
{
    private readonly IScalerMetadataAccessor _metadataAccessor = Substitute.For<IScalerMetadataAccessor>();
    private readonly ConfigureTaskHubOptions _configure;

    public ConfigureTaskHubOptionsTest()
        => _configure = new ConfigureTaskHubOptions(_metadataAccessor);

    [Fact]
    public void GivenNullScalerMetadataAccessor_WhenCreatingConfigure_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new ConfigureTaskHubOptions(null!));

    [Fact]
    public void GivenNullOptions_WhenConfiguring_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => _configure.Configure(null!));

    [Fact]
    public void GivenNullScalerMetadata_WhenConfiguring_ThenThrowInvalidOperationException()
    {
        _ = _metadataAccessor.ScalerMetadata.Returns(default(ScalerMetadata));
        _ = Assert.Throws<InvalidOperationException>(() => _configure.Configure(new TaskHubOptions()));
    }

    [Fact]
    public void GivenScalerMetadata_WhenConfiguring_ThenCopyProperties()
    {
        ScalerMetadata expected = new()
        {
            MaxActivitiesPerWorker = 17,
            MaxOrchestrationsPerWorker = 5,
            TaskHubName = "UnitTest",
            UseTablePartitionManagement = true,
        };

        _ = _metadataAccessor.ScalerMetadata.Returns(expected);

        TaskHubOptions actual = new();
        _configure.Configure(actual);

        Assert.Equal(expected.MaxActivitiesPerWorker, actual.MaxActivitiesPerWorker);
        Assert.Equal(expected.MaxOrchestrationsPerWorker, actual.MaxOrchestrationsPerWorker);
        Assert.Equal(expected.TaskHubName, actual.TaskHubName);
        Assert.Equal(expected.UseTablePartitionManagement, actual.UseTablePartitionManagement);
    }
}
