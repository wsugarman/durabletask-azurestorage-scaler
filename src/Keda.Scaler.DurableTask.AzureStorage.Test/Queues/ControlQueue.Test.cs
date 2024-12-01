// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.TaskHub;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Queues;

public class ControlQueueTest
{
    [Fact]
    public void GivenNullTaskHub_WhenGettingControlQueueName_ThenThrowArgumentException()
        => Assert.Throws<ArgumentNullException>(() => LeasesContainer.GetName(null!));

    [Theory]
    [InlineData("")]
    [InlineData("  \t  ")]
    public void GivenEmptyOrWhiteSpaceTaskHub_WhenGettingControlQueueName_ThenThrowArgumentException(string taskHub)
        => Assert.Throws<ArgumentException>(() => LeasesContainer.GetName(taskHub));

    [Theory]
    [InlineData(-2)]
    [InlineData(19)]
    public void GivenInvalidPartition_WhenGettingControlQueueName_ThenThrowArgumentOutOfRangeException(int partition)
        => Assert.Throws<ArgumentOutOfRangeException>(() => ControlQueue.GetName("foo", partition));

    [Theory]
    [InlineData("foo-control-00", "foo", 0)]
    [InlineData("bar-control-07", "Bar", 7)]
    [InlineData("baz-control-15", "BAZ", 15)]
    public void GivenTaskHub_WhenGettingControlQueueName_ThenReturnExpectedValue(string expected, string taskHub, int partition)
        => Assert.Equal(expected, ControlQueue.GetName(taskHub, partition));
}
