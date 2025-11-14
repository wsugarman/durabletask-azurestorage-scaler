// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.Metadata;
using Keda.Scaler.DurableTask.AzureStorage.TaskHubs;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHubs;

[TestClass]
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

    [TestMethod]
    public void GivenNullOptionsSnapshot_WhenCreatingConfigure_ThenThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ConfigureTaskHubOptions(null!));

        IOptionsSnapshot<ScalerOptions> nullSnapshot = Substitute.For<IOptionsSnapshot<ScalerOptions>>();
        _ = nullSnapshot.Get(default).Returns(default(ScalerOptions));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ConfigureTaskHubOptions(nullSnapshot));
    }

    [TestMethod]
    public void GivenNullOptions_WhenConfiguring_ThenThrowArgumentNullException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => _configure.Configure(null!));

    [TestMethod]
    public void GivenScalerMetadata_WhenConfiguring_ThenCopyProperties()
    {
        _scalerOptions.MaxActivitiesPerWorker = 17;
        _scalerOptions.MaxOrchestrationsPerWorker = 5;
        _scalerOptions.TaskHubName = "UnitTest";
        _scalerOptions.UseTablePartitionManagement = true;

        TaskHubOptions actual = new();
        _configure.Configure(actual);

        Assert.AreEqual(_scalerOptions.MaxActivitiesPerWorker, actual.MaxActivitiesPerWorker);
        Assert.AreEqual(_scalerOptions.MaxOrchestrationsPerWorker, actual.MaxOrchestrationsPerWorker);
        Assert.AreEqual(_scalerOptions.TaskHubName, actual.TaskHubName);
        Assert.AreEqual(_scalerOptions.UseTablePartitionManagement, actual.UseTablePartitionManagement);
    }
}
