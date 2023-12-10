// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.Common;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Common;

public class EnvironmentCacheTest
{
    [Fact]
    public void GivenNullEnvironment_WhenCreatingEnvironmentCache_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new EnvironmentCache(null!));

    [Fact]
    public void GivenEnvironmentCacheWithChangingVariable_WhenGettingVariable_ThenReturnSameValue()
    {
        MockEnvironment env = new();
        EnvironmentCache cache = new(env);

        env.SetEnvironmentVariable("2", "two");
        Assert.Equal("two", env.GetVariable("2"));
        Assert.Equal("two", cache.GetVariable("2"));

        env.SetEnvironmentVariable("2", "deux");
        Assert.Equal("deux", env.GetVariable("2"));
        Assert.Equal("two", cache.GetVariable("2"));
    }
}
