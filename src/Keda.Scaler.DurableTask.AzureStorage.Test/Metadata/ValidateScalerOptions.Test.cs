// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Azure.Identity;
using Keda.Scaler.DurableTask.AzureStorage.Clients;
using Keda.Scaler.DurableTask.AzureStorage.Metadata;
using Microsoft.Extensions.Options;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Metadata;

public class ValidateScalerOptionsTest
{
    private readonly ValidateScalerOptions _validate = new();

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    [InlineData("123-456-789")]
    public void GivenConnectionStringWithClientId_WhenValidatingOptions_ThenReturnFailure(string clientId)
    {
        GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(
            nameof(ScalerOptions.ClientId),
            m =>
            {
                m.ClientId = clientId;
                m.Connection = "foo=bar";
            });
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    [InlineData(nameof(CloudEnvironment.AzurePublicCloud))]
    public void GivenConnectionStringWithCloud_WhenValidatingOptions_ThenReturnFailure(string cloud)
    {
        GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(
            nameof(ScalerOptions.Cloud),
            m =>
            {
                m.Cloud = cloud;
                m.Connection = "foo=bar";
            });
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    [InlineData(AzureStorageServiceUri.PublicSuffix)]
    public void GivenConnectionStringWithEndpointSuffix_WhenValidatingOptions_ThenReturnFailure(string endpointSuffix)
    {
        GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(
            nameof(ScalerOptions.EndpointSuffix),
            m =>
            {
                m.Connection = "foo=bar";
                m.EndpointSuffix = endpointSuffix;
            });
    }

    [Fact]
    public void GivenConnectionStringWithEntraEndpoint_WhenValidatingOptions_ThenReturnFailure()
    {
        GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(
            nameof(ScalerOptions.EntraEndpoint),
            m =>
            {
                m.Connection = "foo=bar";
                m.EntraEndpoint = new Uri("https://login.microsoftonline.com", UriKind.Absolute);
            });
    }

    [Fact]
    public void GivenConnectionStringWithManagedIdentity_WhenValidatingOptions_ThenReturnFailure()
    {
        GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(
            nameof(ScalerOptions.UseManagedIdentity),
            m =>
            {
                m.Connection = "foo=bar";
                m.UseManagedIdentity = true;
            });
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    public void GivenBlankConnection_WhenValidatingOptions_ThenReturnFailure(string connection)
    {
        GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(
            nameof(ScalerOptions.Connection),
            m => m.Connection = connection);
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    public void GivenBlankConnectionEnv_WhenValidatingOptions_ThenReturnFailure(string connectionEnv)
    {
        GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(
            nameof(ScalerOptions.ConnectionFromEnv),
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
                m.UseManagedIdentity = true;
            });
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    public void GivenBlankAccountName_WhenValidatingOptions_ThenReturnFailure(string accountName)
    {
        GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(
            nameof(ScalerOptions.AccountName),
            m =>
            {
                m.AccountName = accountName;
                m.UseManagedIdentity = true;
            });
    }

    [Fact]
    public void GivenUnresolvedCloud_WhenValidatingOptions_ThenReturnFailure()
    {
        GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(
            "foobar",
            o =>
            {
                o.AccountName = "unittest";
                o.Cloud = "foobar";
                o.UseManagedIdentity = true;
            });
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    public void GivenPrivateCloudWithoutEndpointSuffix_WhenValidatingOptions_ThenReturnFailure(string endpointSuffix)
    {
        GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(
            nameof(ScalerOptions.EndpointSuffix),
            m =>
            {
                m.AccountName = "unittest";
                m.Cloud = nameof(CloudEnvironment.Private);
                m.EndpointSuffix = endpointSuffix;
                m.EntraEndpoint = new Uri("https://unit.test.login");
                m.UseManagedIdentity = true;
            });
    }

    [Fact]
    public void GivenPrivateCloudWithoutEntraEndpoint_WhenValidatingOptions_ThenReturnFailure()
    {
        GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(
            nameof(ScalerOptions.EntraEndpoint),
            m =>
            {
                m.AccountName = "unittest";
                m.Cloud = nameof(CloudEnvironment.Private);
                m.EndpointSuffix = "core.unit.test";
                m.EntraEndpoint = null;
                m.UseManagedIdentity = true;
            });
    }

    [Fact]
    public void GivenNonPrivateCloudWithEndpointSuffix_WhenValidatingOptions_ThenReturnFailure()
    {
        GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(
            nameof(ScalerOptions.EndpointSuffix),
            m =>
            {
                m.AccountName = "unittest";
                m.Cloud = nameof(CloudEnvironment.AzurePublicCloud);
                m.EndpointSuffix = AzureStorageServiceUri.USGovernmentSuffix;
                m.UseManagedIdentity = true;
            });
    }

    [Fact]
    public void GivenNonPrivateCloudWithEntraEndpoint_WhenValidatingOptions_ThenReturnFailure()
    {
        GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(
            nameof(ScalerOptions.EntraEndpoint),
            m =>
            {
                m.AccountName = "unittest";
                m.Cloud = nameof(CloudEnvironment.AzurePublicCloud);
                m.EntraEndpoint = AzureAuthorityHosts.AzureChina;
                m.UseManagedIdentity = true;
            });
    }

    [Fact]
    public void GivenNoManagedIdentity_WhenValidatingOptions_ThenReturnFailure()
    {
        GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(
            "Managed identity",
            m =>
            {
                m.AccountName = "unittest";
                m.UseManagedIdentity = false;
            });
    }

    private void GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(string failureSnippet, Action<ScalerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        ScalerOptions options = new();
        configure(options);

        ValidateOptionsResult result = _validate.Validate(Options.DefaultName, options);

        Assert.False(result.Succeeded);
        Assert.True(result.Failed);

        string failureMessage = Assert.Single(result.Failures);
        Assert.Contains(failureSnippet, failureMessage, StringComparison.Ordinal);
    }
}
