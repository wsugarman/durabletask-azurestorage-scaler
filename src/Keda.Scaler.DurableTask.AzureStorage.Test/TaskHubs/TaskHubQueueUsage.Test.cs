// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.TaskHubs;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHubs;

public class TaskHubQueueUsageTest
{
    [Fact]
    public void GivenNullControlQueueMessages_WhenCreatingTaskHubQueueUsage_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new TaskHubQueueUsage(null!, 3));

    [Theory]
    [InlineData(-1)]
    [InlineData(1, 2, -3)]
    public void GivenInvalidControlQueueMessageCount_WhenCreatingTaskHubQueueUsage_ThenThrowArgumentOutOfRangeException(params int[] controlQueueMessages)
        => Assert.Throws<ArgumentOutOfRangeException>(() => new TaskHubQueueUsage(controlQueueMessages, 3));

    [Fact]
    public void GivenInvalidWorkItemQueueMessageCount_WhenCreatingTaskHubQueueUsage_ThenThrowArgumentOutOfRangeException()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new TaskHubQueueUsage([], -2));

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 0, 0, 0)]
    [InlineData(true, 2)]
    [InlineData(true, 1, 0)]
    [InlineData(true, 0, 1)]
    [InlineData(true, 4, 1, 2, 3, 4)]
    public void GivenUsage_WhenQueryingActivity_ThenReturnCorrespondingValue(bool expected, int workItems, params int[] control)
        => Assert.Equal(expected, new TaskHubQueueUsage(control, workItems).HasActivity);
}
