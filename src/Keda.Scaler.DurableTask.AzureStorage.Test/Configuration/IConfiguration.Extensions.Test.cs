// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Keda.Scaler.DurableTask.AzureStorage.Configuration;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Configuration;

public class IConfigurationExtensionsTest
{
    [Fact]
    public void GivenNullConfiguration_WhenGettingSection_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => IConfigurationExtensions.GetOrCreate<Example>(null!));

    [Fact]
    public void GivenEmptyConfiguration_WhenGettingSection_ThenReturnNewObject()
    {
        IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        Example actual = config.GetOrCreate<Example>();

        Assert.NotNull(actual);
        Assert.Equal(default, actual.Duration);
        Assert.Equal(default, actual.Number);
        Assert.Equal(default, actual.Word);
    }

    [Fact]
    public void GivenKnownKeys_WhenGettingSection_ThenBindFoundKeys()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new KeyValuePair<string, string?>[]
                {
                    new("section:duration", "01:00:00"),
                    new("section:number", "42"),
                })
            .Build();

        Example actual = config.GetSection("section").GetOrCreate<Example>();

        Assert.NotNull(actual);
        Assert.Equal(TimeSpan.FromHours(1), actual.Duration);
        Assert.Equal(42, actual.Number);
        Assert.Equal(default, actual.Word);
    }

    private sealed class Example
    {
        public TimeSpan? Duration { get; set; }

        public int Number { get; set; }

        public string? Word { get; set; }
    }
}
