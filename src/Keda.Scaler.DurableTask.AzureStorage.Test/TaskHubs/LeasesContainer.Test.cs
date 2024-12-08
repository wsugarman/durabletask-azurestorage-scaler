// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.TaskHubs;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHubs;

public class LeasesContainerTest
{
    [Fact]
    public void GivenNullTaskHub_WhenGettingLeasesContainerName_ThenThrowArgumentException()
        => Assert.Throws<ArgumentNullException>(() => LeasesContainer.GetName(null!));

    [Theory]
    [InlineData("")]
    [InlineData("  \t  ")]
    public void GivenEmptyOrWhiteSpaceTaskHub_WhenGettingLeasesContainerName_ThenThrowArgumentException(string taskHub)
        => Assert.Throws<ArgumentException>(() => LeasesContainer.GetName(taskHub));

    [Theory]
    [InlineData("foo-leases", "foo")]
    [InlineData("bar-leases", "bar")]
    [InlineData("baz-leases", "BAZ")]
    public void GivenTaskHub_WhenGettingLeasesContainerName_ThenReturnExpectedValue(string expected, string taskHub)
        => Assert.Equal(expected, LeasesContainer.GetName(taskHub));
}
