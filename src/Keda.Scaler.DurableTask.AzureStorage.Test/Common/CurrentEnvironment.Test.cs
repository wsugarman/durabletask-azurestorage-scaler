// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.Common;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Common;

public sealed class CurrentEnvironmentTest : IDisposable
{
    private readonly string _key = Guid.NewGuid().ToString();
    private readonly string _value = Guid.NewGuid().ToString();
    private readonly string? _previous;

    public CurrentEnvironmentTest()
    {
        _previous = Environment.GetEnvironmentVariable(_key, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable(_key, _value, EnvironmentVariableTarget.Process);
    }

    public void Dispose()
        => Environment.SetEnvironmentVariable(_key, _previous);

    [Fact]
    public void GivenEnvironmentVariableInProcess_WhenGettingTheVariable_ThenReturnTheExpectedValue()
        => Assert.Equal(_value, ProcessEnvironment.Current.GetVariable(_key));

    [Fact]
    public void GivenCache_WhenGettingTheVariable_ThenReturnSameValue()
    {
        EnvironmentCache cache = new();

        Assert.Equal(_value, cache.GetVariable(_key));
        Environment.SetEnvironmentVariable(_key, Guid.NewGuid().ToString(), EnvironmentVariableTarget.Process);
        Assert.Equal(_value, cache.GetVariable(_key));
    }
}
