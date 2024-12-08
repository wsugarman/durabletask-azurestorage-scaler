// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.Metadata;
using Keda.Scaler.DurableTask.AzureStorage.TaskHubs;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHubs;

public class ConfigureTaskHubOptionsTest
{
    private readonly ScalerOptions _scalerOptions = new();
    private readonly ConfigureTaskHubOptions _configure;

    public ConfigureTaskHubOptionsTest()
    {
        IOptionsSnapshot<ScalerOptions> snapshot = Substitute.For<IOptionsSnapshot<ScalerOptions>>();
        _ = snapshot.Get(default).Returns(_scalerOptions);
        _configure = new(snapshot);
    }

    [Fact]
    public void GivenNullOptionsSnapshot_WhenCreatingConfigure_ThenThrowArgumentNullException()
    {
        _ = Assert.Throws<ArgumentNullException>(() => new ConfigureTaskHubOptions(null!));

        IOptionsSnapshot<ScalerOptions> nullSnapshot = Substitute.For<IOptionsSnapshot<ScalerOptions>>();
        _ = nullSnapshot.Get(default).Returns(default(ScalerOptions));
        _ = Assert.Throws<ArgumentNullException>(() => new ConfigureTaskHubOptions(nullSnapshot));
    }

    [Fact]
    public void GivenNullOptions_WhenConfiguring_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => _configure.Configure(null!));

    [Fact]
    public void GivenScalerMetadata_WhenConfiguring_ThenCopyProperties()
    {
        _scalerOptions.MaxActivitiesPerWorker = 17;
        _scalerOptions.MaxOrchestrationsPerWorker = 5;
        _scalerOptions.TaskHubName = "UnitTest";
        _scalerOptions.UseTablePartitionManagement = true;

        TaskHubOptions actual = new();
        _configure.Configure(actual);

        Assert.Equal(_scalerOptions.MaxActivitiesPerWorker, actual.MaxActivitiesPerWorker);
        Assert.Equal(_scalerOptions.MaxOrchestrationsPerWorker, actual.MaxOrchestrationsPerWorker);
        Assert.Equal(_scalerOptions.TaskHubName, actual.TaskHubName);
        Assert.Equal(_scalerOptions.UseTablePartitionManagement, actual.UseTablePartitionManagement);
    }
}
