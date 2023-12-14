// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.TaskHub;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.TaskHub;

public class AzureStorageTaskHubInfoTest
{
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(16)]
    [InlineData(42)]
    public void GivenInvalidPartitionCount_WhenCreatingAzureStorageTaskHubInfo_ThenThrowArgumentOutOfRangeException(int partitionCount)
        => Assert.Throws<ArgumentOutOfRangeException>(() => new AzureStorageTaskHubInfo(DateTimeOffset.UtcNow, partitionCount, "foo"));

    [Fact]
    public void GivenNullTaskHubName_WhenCreatingAzureStorageTaskHubInfo_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new AzureStorageTaskHubInfo(DateTimeOffset.UtcNow, 4, null!));

    [Theory]
    [InlineData("")]
    [InlineData("\t")]
    public void GivenEmptyOrWhiteSpaceTaskHubName_WhenCreatingAzureStorageTaskHubInfo_ThenThrowArgumentException(string taskHubName)
        => Assert.Throws<ArgumentException>(() => new AzureStorageTaskHubInfo(DateTimeOffset.UtcNow, 4, taskHubName));
}
