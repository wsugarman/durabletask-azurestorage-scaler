// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.ComponentModel.DataAnnotations;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Keda.Scaler.DurableTask.AzureStorage.Common;
using Keda.Scaler.DurableTask.AzureStorage.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test;

public class ScalerMetadataTest
{
    private readonly MockEnvironment _environment = new();
    private readonly IServiceProvider _serviceProvider;

    public ScalerMetadataTest()
    {
        _serviceProvider = new ServiceCollection()
            .AddSingleton<IProcessEnvironment>(_environment)
            .BuildServiceProvider();
    }

    [Theory]
    [InlineData(CloudEnvironment.AzurePublicCloud, null)]
    [InlineData(CloudEnvironment.AzurePublicCloud, nameof(CloudEnvironment.AzurePublicCloud))]
    [InlineData(CloudEnvironment.AzureUSGovernmentCloud, nameof(CloudEnvironment.AzureUSGovernmentCloud))]
    [InlineData(CloudEnvironment.AzureChinaCloud, nameof(CloudEnvironment.AzureChinaCloud))]
    [InlineData(CloudEnvironment.AzureGermanCloud, nameof(CloudEnvironment.AzureGermanCloud))]
    [InlineData(CloudEnvironment.Unknown, "foo")]
    public void GivenCloudString_WhenGettingCloudEnvironment_ThenReturnCorrespondingValue(CloudEnvironment expected, string? cloud)
        => Assert.Equal(expected, new ScalerMetadata { Cloud = cloud }.CloudEnvironment);

    [Fact]
    public void GivenNullEnvironment_WhenResolvingConnectionString_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new ScalerMetadata().ResolveConnectionString(null!));

    [Theory]
    [InlineData(null, ScalerMetadata.DefaultConnectionEnvironmentVariable, "one=1")]
    [InlineData("MY_CONNECTION", "MY_CONNECTION", "one=1")]
    public void GivenConnectionEnvironmentVariable_WhenResolvingConnectionString_ThenLookUpCorrectValue(string? connectionFromEnv, string key, string value)
    {
        _environment.SetEnvironmentVariable(key, value);

        ScalerMetadata metadata = new() { ConnectionFromEnv = connectionFromEnv };
        Assert.Equal(value, metadata.ResolveConnectionString(_environment));
    }

    [Fact]
    public void GivenProvidedConnectionString_WhenResolvingConnectionString_ThenUseInsteadOfEnvironmentVariable()
    {
        _environment.SetEnvironmentVariable(ScalerMetadata.DefaultConnectionEnvironmentVariable, "one=1");
        _environment.SetEnvironmentVariable("MY_CONNECTION", "two=2");

        ScalerMetadata metadata = new() { Connection = "three=3", ConnectionFromEnv = "MY_CONNECTION" };
        Assert.Equal("three=3", metadata.ResolveConnectionString(_environment));
    }

    [Fact]
    public void GivenNullContext_WhenValidating_ThenThrowArgumentNullException()
    {
        ScalerMetadata metadata = new();
        _ = Assert.Throws<ArgumentNullException>(() => ((IValidatableObject)metadata).Validate(null!));
    }

    [Fact]
    public void GivenMissingProcessEnvironmentService_WhenValidating_ThenThrowInvalidOperationException()
    {
        ScalerMetadata metadata = new();
        ValidationContext context = new(metadata, new ServiceCollection().BuildServiceProvider(), null);
        _ = Assert.Throws<InvalidOperationException>(() => ((IValidatableObject)metadata).Validate(context));
    }

    [Fact]
    public void GivenPublicCloudWithAadEndpoint_WhenValidating_ThenThrowValidationException()
    {
        ScalerMetadata metadata = new()
        {
            ActiveDirectoryEndpoint = new Uri("https://example.aad"),
            Cloud = nameof(CloudEnvironment.AzurePublicCloud),
        };

        _ = Assert.Throws<ValidationException>(() => metadata.ThrowIfInvalid(_serviceProvider));
    }

    [Fact]
    public void GivenPublicCloudWithEndpointSuffix_WhenValidating_ThenThrowValidationException()
    {
        ScalerMetadata metadata = new()
        {
            Cloud = nameof(CloudEnvironment.AzurePublicCloud),
            EndpointSuffix = "example",
        };

        _ = Assert.Throws<ValidationException>(() => metadata.ThrowIfInvalid(_serviceProvider));
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

        _ = Assert.Throws<ValidationException>(() => metadata.ThrowIfInvalid(_serviceProvider));
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

        _ = Assert.Throws<ValidationException>(() => metadata.ThrowIfInvalid(_serviceProvider));
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

        _ = Assert.Throws<ValidationException>(() => metadata.ThrowIfInvalid(_serviceProvider));
    }

    [Fact]
    public void GivenAccountWithUnknownCloud_WhenValidating_ThenThrowValidationException()
    {
        ScalerMetadata metadata = new()
        {
            AccountName = "example",
            Cloud = "other",
        };

        _ = Assert.Throws<ValidationException>(() => metadata.ThrowIfInvalid(_serviceProvider));
    }

    [Fact]
    public void GivenAccountWithConnection_WhenValidating_ThenThrowValidationException()
    {
        ScalerMetadata metadata = new()
        {
            AccountName = "example",
            Connection = "Connection=property",
        };

        _ = Assert.Throws<ValidationException>(() => metadata.ThrowIfInvalid(_serviceProvider));
    }

    [Fact]
    public void GivenAccountWithConnectionFromEnv_WhenValidating_ThenThrowValidationException()
    {
        ScalerMetadata metadata = new()
        {
            AccountName = "example",
            ConnectionFromEnv = "CONNECTION",
        };

        _ = Assert.Throws<ValidationException>(() => metadata.ThrowIfInvalid(_serviceProvider));
    }

    [Fact]
    public void GivenAccountWithNoIdentityBasedConnectionClientId_WhenValidating_ThenThrowValidationException()
    {
        ScalerMetadata metadata = new()
        {
            AccountName = "example",
            ClientId = Guid.NewGuid().ToString(),
            UseManagedIdentity = false,
        };

        _ = Assert.Throws<ValidationException>(() => metadata.ThrowIfInvalid(_serviceProvider));
    }

    [Fact]
    public void GivenConnectionWithClientId_WhenValidating_ThenThrowValidationException()
    {
        ScalerMetadata metadata = new()
        {
            AccountName = null,
            ClientId = Guid.NewGuid().ToString(),
        };

        _ = Assert.Throws<ValidationException>(() => metadata.ThrowIfInvalid(_serviceProvider));
    }

    [Fact]
    public void GivenConnectionWithCloud_WhenValidating_ThenThrowValidationException()
    {
        ScalerMetadata metadata = new()
        {
            AccountName = null,
            Cloud = nameof(CloudEnvironment.AzureUSGovernmentCloud),
        };

        _ = Assert.Throws<ValidationException>(() => metadata.ThrowIfInvalid(_serviceProvider));
    }

    [Fact]
    public void GivenConnectionWithConnectionBasedIdentity_WhenValidating_ThenThrowValidationException()
    {
        ScalerMetadata metadata = new()
        {
            AccountName = null,
            UseManagedIdentity = true,
        };

        _ = Assert.Throws<ValidationException>(() => metadata.ThrowIfInvalid(_serviceProvider));
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

        _ = Assert.Throws<ValidationException>(() => metadata.ThrowIfInvalid(_serviceProvider));
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

        _ = Assert.Throws<ValidationException>(() => metadata.ThrowIfInvalid(_serviceProvider));
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
        _environment.SetEnvironmentVariable(connectionFromEnv ?? ScalerMetadata.DefaultConnectionEnvironmentVariable, value);

        ScalerMetadata metadata = new()
        {
            AccountName = null,
            ConnectionFromEnv = connectionFromEnv,
        };

        _ = Assert.Throws<ValidationException>(() => metadata.ThrowIfInvalid(_serviceProvider));
    }
}
