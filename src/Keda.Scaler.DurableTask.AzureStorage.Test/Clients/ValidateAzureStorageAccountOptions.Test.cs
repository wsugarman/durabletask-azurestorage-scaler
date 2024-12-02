// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Azure.Identity;
using Keda.Scaler.DurableTask.AzureStorage.Clients;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Clients;

public class ValidateAzureStorageAccountOptionsTest
{
    private readonly ScalerMetadata _metadata = new();
    private readonly ValidateAzureStorageAccountOptions _validate;

    public ValidateAzureStorageAccountOptionsTest()
    {
        IScalerMetadataAccessor scalerMetadataAccessor = Substitute.For<IScalerMetadataAccessor>();
        _ = scalerMetadataAccessor.ScalerMetadata.Returns(_metadata);
        _validate = new(scalerMetadataAccessor);
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    [InlineData("123-456-789")]
    public void GivenConnectionStringWithClientId_WhenValidatingOptions_ThenReturnFailure(string clientId)
    {
        GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(
            nameof(ScalerMetadata.ClientId),
            m => m.ClientId = clientId,
            o => o.ConnectionString = "foo=bar");
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    [InlineData(CloudEnvironment.AzurePublicCloud)]
    public void GivenConnectionStringWithCloud_WhenValidatingOptions_ThenReturnFailure(string cloud)
    {
        GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(
            nameof(ScalerMetadata.Cloud),
            m => m.Cloud = cloud,
            o => o.ConnectionString = "foo=bar");
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    [InlineData(AzureStorageServiceUri.PublicSuffix)]
    public void GivenConnectionStringWithEndpointSuffix_WhenValidatingOptions_ThenReturnFailure(string endpointSuffix)
    {
        GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(
            nameof(ScalerMetadata.EndpointSuffix),
            m => m.EndpointSuffix = endpointSuffix,
            o => o.ConnectionString = "foo=bar");
    }

    [Fact]
    public void GivenConnectionStringWithEntraEndpoint_WhenValidatingOptions_ThenReturnFailure()
    {
        GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(
            nameof(ScalerMetadata.EntraEndpoint),
            m => m.EntraEndpoint = new Uri("https://login.microsoftonline.com", UriKind.Absolute),
            o => o.ConnectionString = "foo=bar");
    }

    [Fact]
    public void GivenConnectionStringWithManagedIdentity_WhenValidatingOptions_ThenReturnFailure()
    {
        GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(
            nameof(ScalerMetadata.UseManagedIdentity),
            m => m.UseManagedIdentity = true,
            o => o.ConnectionString = "foo=bar");
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    public void GivenBlankConnection_WhenValidatingOptions_ThenReturnFailure(string connection)
    {
        GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(
            nameof(ScalerMetadata.Connection),
            m => m.Connection = connection,
            o => o.ConnectionString = connection);
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    public void GivenBlankConnectionEnv_WhenValidatingOptions_ThenReturnFailure(string connectionEnv)
    {
        GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(
            nameof(ScalerMetadata.ConnectionFromEnv),
            m => m.ConnectionFromEnv = connectionEnv);
    }

    [Fact]
    public void GivenBothConnectionAndConectionEnv_WhenValidatingOptions_ThenReturnFailure()
    {
        GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(
            "Multiple Azure Storage connection values",
            m =>
            {
                m.Connection = "Foo=Bar";
                m.ConnectionFromEnv = "ExampleEnvVariable";
            });
    }

    [Theory]
    [InlineData("ExampleEnvVariable")]
    [InlineData(null)]
    public void GivenUnresolvedConnectionString_WhenValidatingOptions_ThenReturnFailure(string? variableName)
    {
        GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(
            variableName ?? AzureStorageAccountOptions.DefaultConnectionEnvironmentVariable,
            m => m.ConnectionFromEnv = variableName);
    }

    [Theory]
    [InlineData("Foo=Bar", null)]
    [InlineData(null, "ExampleEnvVariable")]
    [InlineData("Foo=Bar", "ExampleEnvVariable")]
    public void GivenAccountNameWithConnectionString_WhenValidatingOptions_ThenReturnFailure(string? connection, string? connectionEnv)
    {
        GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(
            "Multiple Azure Storage connection values",
            m =>
            {
                m.AccountName = "unittest";
                m.Connection = connection;
                m.ConnectionFromEnv = connectionEnv;
            },
            o => o.AccountName = "unittest");
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    public void GivenBlankAccountName_WhenValidatingOptions_ThenReturnFailure(string accountName)
    {
        GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(
            nameof(ScalerMetadata.AccountName),
            m => m.AccountName = accountName,
            o => o.AccountName = accountName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    public void GivenPrivateCloudWithoutEndpointSuffix_WhenValidatingOptions_ThenReturnFailure(string endpointSuffix)
    {
        GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(
            nameof(ScalerMetadata.EndpointSuffix),
            m =>
            {
                m.AccountName = "unittest";
                m.Cloud = CloudEnvironment.Private;
                m.EndpointSuffix = endpointSuffix;
            },
            o =>
            {
                o.AccountName = "unittest";
                o.EndpointSuffix = endpointSuffix;
            });
    }

    [Fact]
    public void GivenPrivateCloudWithoutEntraEndpoint_WhenValidatingOptions_ThenReturnFailure()
    {
        GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(
            nameof(ScalerMetadata.EntraEndpoint),
            m =>
            {
                m.AccountName = "unittest";
                m.Cloud = CloudEnvironment.Private;
                m.EntraEndpoint = null;
            },
            o => o.AccountName = "unittest");
    }

    [Fact]
    public void GivenNonPrivateCloudWithEndpointSuffix_WhenValidatingOptions_ThenReturnFailure()
    {
        GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(
            nameof(ScalerMetadata.EndpointSuffix),
            m =>
            {
                m.AccountName = "unittest";
                m.Cloud = CloudEnvironment.AzurePublicCloud;
                m.EndpointSuffix = AzureStorageServiceUri.USGovernmentSuffix;
            },
            o =>
            {
                o.AccountName = "unittest";
                o.EndpointSuffix = AzureStorageServiceUri.PublicSuffix;
            });
    }

    [Fact]
    public void GivenNonPrivateCloudWithMissingEndpointSuffix_WhenValidatingOptions_ThenReturnFailure()
    {
        GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(
            nameof(ScalerMetadata.Cloud),
            m =>
            {
                m.AccountName = "unittest";
                m.Cloud = "UnknownCloud";
            },
            o =>
            {
                o.AccountName = "unittest";
                o.EndpointSuffix = null;
            });
    }

    [Fact]
    public void GivenNonPrivateCloudWithEntraEndpoint_WhenValidatingOptions_ThenReturnFailure()
    {
        GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(
            nameof(ScalerMetadata.EntraEndpoint),
            m =>
            {
                m.AccountName = "unittest";
                m.Cloud = CloudEnvironment.AzurePublicCloud;
                m.EntraEndpoint = AzureAuthorityHosts.AzureChina;
            },
            o =>
            {
                o.AccountName = "unittest";
                o.EndpointSuffix = AzureStorageServiceUri.PublicSuffix;
            });
    }

    [Fact]
    public void GivenNoManagedIdentityWithClientId_WhenValidatingOptions_ThenReturnFailure()
    {
        GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(
            nameof(ScalerMetadata.ClientId),
            m =>
            {
                m.AccountName = "unittest";
                m.ClientId = "123-456-789";
                m.UseManagedIdentity = false;
            },
            o => o.AccountName = "unittest");
    }

    private void GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(string failureSnippet, Action<ScalerMetadata> configureMetadata, Action<AzureStorageAccountOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(configureMetadata);

        const string ConnectionString = "foo=bar";

        _metadata.Connection = ConnectionString;
        configureMetadata(_metadata);

        AzureStorageAccountOptions options = new() { ConnectionString = ConnectionString };
        configureOptions?.Invoke(options);

        ValidateOptionsResult result = _validate.Validate(Options.DefaultName, options);

        Assert.False(result.Succeeded);
        Assert.True(result.Failed);

        string failureMessage = Assert.Single(result.Failures);
        Assert.Contains(failureSnippet, failureMessage, StringComparison.Ordinal);
    }
}
