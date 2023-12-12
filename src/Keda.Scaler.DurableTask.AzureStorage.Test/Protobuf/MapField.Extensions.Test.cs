// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Google.Protobuf.Collections;
using Keda.Scaler.DurableTask.AzureStorage.Protobuf;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Protobuf;

public class MapFieldExtensionsTest
{
    [Fact]
    public void GivenNullMapField_WhenCreatingAConfiguration_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => MapFieldExtensions.ToConfiguration(null!));

    [Theory]
    [InlineData("key", "value", "key", "value")]
    [InlineData("upperCASE", "VALUE", "UPPERcase", "VALUE")]
    [InlineData("missing", null, "key", "unused")]
    [InlineData("looks:like:a:section", "another-value", "looks:LIKE:a:section", "another-value")]
    public void GivenMapField_WhenCreatingAConfiguration_ThenReturnEquivalentConfiguration(string expectedKey, string? expectedValue, string key, string value)
    {
        MapField<string, string> mapField = new() { { key, value } };
        Assert.Equal(expectedValue, mapField.ToConfiguration()[expectedKey]);
    }

    [Theory]
    [InlineData("section", "key", "value", "section:key", "value")]
    [InlineData("secTion", "nested:KEY", "value", "section:NESTED:key", "value")]
    public void GivenMapField_WhenCreatingAConfiguration_ThenReturnEquivalentConfigurationSection(string expectedSection, string expectedKey, string? expectedValue, string key, string value)
    {
        MapField<string, string> mapField = new() { { key, value } };
        Assert.Equal(expectedValue, mapField.ToConfiguration().GetSection(expectedSection)[expectedKey]);
    }
}
