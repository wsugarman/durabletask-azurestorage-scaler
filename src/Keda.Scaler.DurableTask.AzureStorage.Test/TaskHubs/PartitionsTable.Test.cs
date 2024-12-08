// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.TaskHubs;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHubs;

public class PartitionsTableTest
{
    [Fact]
    public void GivenNullTaskHub_WhenGettingPartitionsTableName_ThenThrowArgumentException()
        => Assert.Throws<ArgumentNullException>(() => PartitionsTable.GetName(null!));

    [Theory]
    [InlineData("")]
    [InlineData("  \t  ")]
    public void GivenEmptyOrWhiteSpaceTaskHub_WhenGettingPartitionsTableName_ThenThrowArgumentException(string taskHub)
        => Assert.Throws<ArgumentException>(() => PartitionsTable.GetName(taskHub));

    [Theory]
    [InlineData("fooPartitions", "foo")]
    [InlineData("BarPartitions", "Bar")]
    [InlineData("BAZPartitions", "BAZ")]
    public void GivenTaskHub_WhenGettingPartitionsTableName_ThenReturnExpectedValue(string expected, string taskHub)
        => Assert.Equal(expected, PartitionsTable.GetName(taskHub));
}
