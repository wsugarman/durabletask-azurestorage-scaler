// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.ComponentModel.DataAnnotations;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test;

public class ScalerMetadataTest
{
    [Theory]
    [InlineData(CloudEnvironment.AzurePublicCloud, null)]
    [InlineData(CloudEnvironment.AzurePublicCloud, nameof(CloudEnvironment.AzurePublicCloud))]
    [InlineData(CloudEnvironment.AzureUSGovernmentCloud, nameof(CloudEnvironment.AzureUSGovernmentCloud))]
    [InlineData(CloudEnvironment.AzureChinaCloud, nameof(CloudEnvironment.AzureChinaCloud))]
    [InlineData(CloudEnvironment.AzureGermanCloud, nameof(CloudEnvironment.AzureGermanCloud))]
    [InlineData(CloudEnvironment.Unknown, "foo")]
    public void GivenCloudString_WhenGettingCloudEnvironment_ThenReturnCorrespondingValue(CloudEnvironment expected, string? cloud)
        => Assert.Equal(expected, new ScalerMetadata { Cloud = cloud }.CloudEnvironment);

    [Theory]
    [InlineData(null, ScalerMetadata.DefaultConnectionEnvironmentVariable, "one=1")]
    [InlineData("MY_CONNECTION_1", "MY_CONNECTION_1", "one=1")]
    public void GivenConnectionEnvironmentVariable_WhenResolvingConnectionString_ThenLookUpCorrectValue(string? connectionFromEnv, string key, string value)
    {
        ScalerMetadata metadata = new() { ConnectionFromEnv = connectionFromEnv };
        using (TestEnvironment.SetVariable(key, value))
            Assert.Equal(value, metadata.ConnectionString);

        // Ensure cached
        using (TestEnvironment.SetVariable(key, value + ";more=values"))
            Assert.Equal(value, metadata.ConnectionString);

    }

    [Fact]
    public void GivenProvidedConnectionString_WhenResolvingConnectionString_ThenUseInsteadOfEnvironmentVariable()
    {
        using IDisposable replacement1 = TestEnvironment.SetVariable(ScalerMetadata.DefaultConnectionEnvironmentVariable, "one=1");
        using IDisposable replacement2 = TestEnvironment.SetVariable("MY_CONNECTION_2", "two=2");

        ScalerMetadata metadata = new() { Connection = "three=3", ConnectionFromEnv = "MY_CONNECTION_2" };
        Assert.Equal("three=3", metadata.ConnectionString);
    }

    [Fact]
    public void GivenNullContext_WhenValidating_ThenThrowArgumentNullException()
    {
        ScalerMetadata metadata = new();
        _ = Assert.Throws<ArgumentNullException>(() => ((IValidatableObject)metadata).Validate(null!));
    }

    [Fact]
    public void GivenPublicCloudWithAadEndpoint_WhenValidating_ThenThrowValidationException()
    {
        ScalerMetadata metadata = new()
        {
            ActiveDirectoryEndpoint = new Uri("https://example.aad"),
            Cloud = nameof(CloudEnvironment.AzurePublicCloud),
        };

        Assert.True(new ValidateScalerMetadata().Validate(null, metadata).Failed);
    }

    [Fact]
    public void GivenPublicCloudWithEndpointSuffix_WhenValidating_ThenThrowValidationException()
    {
        ScalerMetadata metadata = new()
        {
            Cloud = nameof(CloudEnvironment.AzurePublicCloud),
            EndpointSuffix = "example",
        };

        Assert.True(new ValidateScalerMetadata().Validate(null, metadata).Failed);
    }

    [Fact]
    public void GivenPrivateCloudWithMissingAadEndpoint_WhenValidating_ThenThrowValidationException()
    {
        ScalerMetadata metadata = new()
        {
            ActiveDirectoryEndpoint = null,
            Cloud = nameof(CloudEnvironment.Private),
            EndpointSuffix = "example",
        };

        Assert.True(new ValidateScalerMetadata().Validate(null, metadata).Failed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("\r\n")]
    public void GivenPrivateCloudWithMissingEndpointSuffix_WhenValidating_ThenThrowValidationException(string? suffix)
    {
        ScalerMetadata metadata = new()
        {
            ActiveDirectoryEndpoint = new Uri("https://example.aad"),
            Cloud = nameof(CloudEnvironment.Private),
            EndpointSuffix = suffix,
        };

        Assert.True(new ValidateScalerMetadata().Validate(null, metadata).Failed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void GivenBlankAccountName_WhenValidating_ThenThrowValidationException(string accountName)
    {
        ScalerMetadata metadata = new()
        {
            AccountName = accountName,
            Cloud = nameof(CloudEnvironment.Private),
        };

        Assert.True(new ValidateScalerMetadata().Validate(null, metadata).Failed);
    }

    [Fact]
    public void GivenAccountWithUnknownCloud_WhenValidating_ThenThrowValidationException()
    {
        ScalerMetadata metadata = new()
        {
            AccountName = "example",
            Cloud = "other",
        };

        Assert.True(new ValidateScalerMetadata().Validate(null, metadata).Failed);
    }

    [Fact]
    public void GivenAccountWithConnection_WhenValidating_ThenThrowValidationException()
    {
        ScalerMetadata metadata = new()
        {
            AccountName = "example",
            Connection = "Connection=property",
        };

        Assert.True(new ValidateScalerMetadata().Validate(null, metadata).Failed);
    }

    [Fact]
    public void GivenAccountWithConnectionFromEnv_WhenValidating_ThenThrowValidationException()
    {
        ScalerMetadata metadata = new()
        {
            AccountName = "example",
            ConnectionFromEnv = "CONNECTION",
        };

        Assert.True(new ValidateScalerMetadata().Validate(null, metadata).Failed);
    }

    [Fact]
    [Obsolete("UseManagedIdentity is deprecated.")]
    public void GivenAccountWithNoManagedIdentityClientId_WhenValidating_ThenThrowValidationException()
    {
        ScalerMetadata metadata = new()
        {
            AccountName = "example",
            ClientId = Guid.NewGuid().ToString(),
            UseManagedIdentity = false,
        };

        Assert.True(new ValidateScalerMetadata().Validate(null, metadata).Failed);
    }

    [Fact]
    public void GivenAccountWithNoWorkloadIdentityClientId_WhenValidating_ThenThrowValidationException()
    {
        ScalerMetadata metadata = new()
        {
            AccountName = "example",
            ClientId = Guid.NewGuid().ToString(),
            UseWorkloadIdentity = false,
        };

        Assert.True(new ValidateScalerMetadata().Validate(null, metadata).Failed);
    }

    [Fact]
    [Obsolete("UseManagedIdentity is deprecated.")]
    public void GivenAccountWithAmbiguousCredentials_WhenValidating_ThenThrowValidationException()
    {
        ScalerMetadata metadata = new()
        {
            AccountName = "example",
            UseManagedIdentity = true,
            UseWorkloadIdentity = true,
        };

        Assert.True(new ValidateScalerMetadata().Validate(null, metadata).Failed);
    }

    [Fact]
    public void GivenConnectionWithClientId_WhenValidating_ThenThrowValidationException()
    {
        ScalerMetadata metadata = new()
        {
            AccountName = null,
            ClientId = Guid.NewGuid().ToString(),
        };

        Assert.True(new ValidateScalerMetadata().Validate(null, metadata).Failed);
    }

    [Fact]
    public void GivenConnectionWithCloud_WhenValidating_ThenThrowValidationException()
    {
        ScalerMetadata metadata = new()
        {
            AccountName = null,
            Cloud = nameof(CloudEnvironment.AzureUSGovernmentCloud),
        };

        Assert.True(new ValidateScalerMetadata().Validate(null, metadata).Failed);
    }

    [Fact]
    [Obsolete("UseManagedIdentity is deprecated.")]
    public void GivenConnectionWithManagedIdentity_WhenValidating_ThenThrowValidationException()
    {
        ScalerMetadata metadata = new()
        {
            AccountName = null,
            UseManagedIdentity = true,
        };

        Assert.True(new ValidateScalerMetadata().Validate(null, metadata).Failed);
    }

    [Fact]
    public void GivenConnectionWithWorkloadIdentity_WhenValidating_ThenThrowValidationException()
    {
        ScalerMetadata metadata = new()
        {
            AccountName = null,
            UseWorkloadIdentity = true,
        };

        Assert.True(new ValidateScalerMetadata().Validate(null, metadata).Failed);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \t ")]
    public void GivenEmptyOrWhiteSpaceConnection_WhenValidating_ThenThrowValidationException(string connection)
    {
        ScalerMetadata metadata = new()
        {
            AccountName = null,
            Connection = connection,
        };

        Assert.True(new ValidateScalerMetadata().Validate(null, metadata).Failed);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \t ")]
    public void GivenEmptyOrWhiteSpaceConnectionFromEnv_WhenValidating_ThenThrowValidationException(string connectionFromEnv)
    {
        ScalerMetadata metadata = new()
        {
            AccountName = null,
            ConnectionFromEnv = connectionFromEnv,
        };

        Assert.True(new ValidateScalerMetadata().Validate(null, metadata).Failed);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(null, "")]
    [InlineData(null, "    ")]
    [InlineData("INVALID", null)]
    [InlineData("INVALID", "")]
    [InlineData("INVALID", "    ")]
    public void GivenNullOrWhiteSpaceResolvedConnection_WhenValidating_ThenThrowValidationException(string? connectionFromEnv, string? value)
    {
        using IDisposable replacement = TestEnvironment.SetVariable(connectionFromEnv ?? ScalerMetadata.DefaultConnectionEnvironmentVariable, value);

        ScalerMetadata metadata = new()
        {
            AccountName = null,
            ConnectionFromEnv = connectionFromEnv,
        };

        Assert.True(new ValidateScalerMetadata().Validate(null, metadata).Failed);
    }
}
