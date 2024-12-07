// Copyright © William Sugarman.
// Licensed under the MIT License.

using Keda.Scaler.DurableTask.AzureStorage.TaskHub;
using System;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHub;

public class WorkItemQueueTest
{
    [Fact]
    public void GivenNullTaskHub_WhenGettingWorkItemQueueName_ThenThrowArgumentException()
        => Assert.Throws<ArgumentNullException>(() => WorkItemQueue.GetName(null!));

    [Theory]
    [InlineData("")]
    [InlineData("  \t  ")]
    public void GivenEmptyOrWhiteSpaceTaskHub_WhenGettingWorkItemQueueName_ThenThrowArgumentException(string taskHub)
        => Assert.Throws<ArgumentException>(() => WorkItemQueue.GetName(taskHub));

    [Theory]
    [InlineData("foo-workitems", "foo")]
    [InlineData("bar-workitems", "bar")]
    [InlineData("baz-workitems", "BAZ")]
    public void GivenTaskHub_WhenGettingWorkItemQueueName_ThenReturnExpectedValue(string expected, string taskHub)
        => Assert.Equal(expected, WorkItemQueue.GetName(taskHub));
}
