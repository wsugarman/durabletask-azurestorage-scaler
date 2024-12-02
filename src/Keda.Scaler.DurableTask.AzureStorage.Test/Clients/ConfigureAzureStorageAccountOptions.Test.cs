// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Azure.Identity;
using Keda.Scaler.DurableTask.AzureStorage.Clients;
using Microsoft.Identity.Client;
using NSubstitute;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Clients;

public class ConfigureAzureStorageAccountOptionsTest
{
    private readonly ScalerMetadata _metadata = new();
    private readonly ConfigureAzureStorageAccountOptions _configure;

    public ConfigureAzureStorageAccountOptionsTest()
    {
        IScalerMetadataAccessor scalerMetadataAccessor = Substitute.For<IScalerMetadataAccessor>();
        _ = scalerMetadataAccessor.ScalerMetadata.Returns(_metadata);
        _configure = new(scalerMetadataAccessor);
    }

    [Theory]
    [InlineData("Key=1", "Key=1", null, null, null)]
    [InlineData("Key=1", "Key=1", "ExampleConnectionString1", "Key=2", "Key=3")]
    [InlineData("Key=1", null, "ExampleConnectionString2", "Key=1", null)]
    [InlineData("Key=1", null, "ExampleConnectionString3", "Key=1", "Key=2")]
    [InlineData("Key=1", null, null, null, "Key=1")]
    public void GivenMetadataWithConnectionString_WhenConfiguringOptions_ThenConfigureConnectionString(string expected, string? connection, string? envKey, string? envValue, string? defaultEnvValue)
    {
        List<IDisposable> disposables = [];

        if (envKey is not null)
            disposables.Add(TestEnvironment.SetVariable(envKey, envValue));

        if (defaultEnvValue is not null)
            disposables.Add(TestEnvironment.SetVariable(AzureStorageAccountOptions.DefaultConnectionEnvironmentVariable, defaultEnvValue));

        _metadata.Connection = connection;
        _metadata.ConnectionFromEnv = envKey;

        AzureStorageAccountOptions options = new();

        try
        {
            _configure.Configure(options);
            Assert.Null(options.AccountName);
            Assert.Equal(expected, options.ConnectionString);
            Assert.Null(options.EndpointSuffix);
            Assert.Null(options.TokenCredential);
        }
        finally
        {
            foreach (IDisposable disposable in disposables)
                disposable.Dispose();
        }
    }

    [Theory]
    [InlineData("Key=1", "Key=2", null, null, null)]
    [InlineData("Key=1", "Key=2", "ExampleConnectionString4", "Key=3", "Key=4")]
    [InlineData("Key=1", null, "ExampleConnectionString5", "Key=2", null)]
    [InlineData("Key=1", null, "ExampleConnectionString6", "Key=3", "Key=4")]
    [InlineData("Key=1", null, null, null, "Key=2")]
    public void GivenMetadataWithAccount_WhenConfiguringOptions_ThenConfigureUriConnection(string accountName, string? connection, string? envKey, string? envValue, string? defaultEnvValue)
    {
        List<IDisposable> disposables = [];

        if (envKey is not null)
            disposables.Add(TestEnvironment.SetVariable(envKey, envValue));

        if (defaultEnvValue is not null)
            disposables.Add(TestEnvironment.SetVariable(AzureStorageAccountOptions.DefaultConnectionEnvironmentVariable, defaultEnvValue));

        _metadata.AccountName = accountName;
        _metadata.Connection = connection;
        _metadata.ConnectionFromEnv = envKey;

        AzureStorageAccountOptions options = new();

        try
        {
            _configure.Configure(options);
            Assert.Equal(accountName, options.AccountName);
            Assert.Null(options.ConnectionString);
            Assert.Equal(AzureStorageServiceUri.PublicSuffix, options.EndpointSuffix);
            Assert.Null(options.TokenCredential);
        }
        finally
        {
            foreach (IDisposable disposable in disposables)
                disposable.Dispose();
        }
    }

    [Theory]
    [InlineData(AzureStorageServiceUri.PublicSuffix, null, null)]
    [InlineData(AzureStorageServiceUri.PublicSuffix, CloudEnvironment.AzurePublicCloud, null)]
    [InlineData(AzureStorageServiceUri.PublicSuffix, "azurepubliccloud", null)]
    [InlineData(AzureStorageServiceUri.USGovernmentSuffix, CloudEnvironment.AzureUSGovernmentCloud, null)]
    [InlineData(AzureStorageServiceUri.USGovernmentSuffix, "AZUREUSGOVERNMENTCLOUD", null)]
    [InlineData(AzureStorageServiceUri.ChinaSuffix, CloudEnvironment.AzureChinaCloud, null)]
    [InlineData(AzureStorageServiceUri.ChinaSuffix, "azureCHINAcloud", null)]
    [InlineData("unit.test.cloud", CloudEnvironment.Private, "unit.test.cloud")]
    [InlineData("unit.test.cloud", "priVATE", "unit.test.cloud")]
    [InlineData(null, "AzureUnknownCloud", null)]
    [InlineData(null, CloudEnvironment.Private, null)]
    public void GivenMetadataWithAccount_WhenConfiguringOptions_ThenConfigureEndpointBasedOnCloud(string? expected, string? cloud, string? endpointSuffix)
    {
        const string AccountName = "unittest";

        _metadata.AccountName = AccountName;
        _metadata.Cloud = cloud;
        _metadata.EndpointSuffix = endpointSuffix;

        AzureStorageAccountOptions options = new();
        _configure.Configure(options);

        Assert.Equal(AccountName, options.AccountName);
        Assert.Null(options.ConnectionString);
        Assert.Equal(expected, options.EndpointSuffix);
        Assert.Null(options.TokenCredential);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("https://entra.unit.test", "12345")]
    public void GivenMetadataWithAccount_WhenConfiguringOptions_ThenConfigureTokenCredential(string? entraEndpoint, string? clientId)
    {
        const string AccountName = "unittest";
        string defaultClientId = Guid.NewGuid().ToString();

        using IDisposable tenant = TestEnvironment.SetVariable("AZURE_TENANT_ID", Guid.NewGuid().ToString());
        using IDisposable client = TestEnvironment.SetVariable("AZURE_CLIENT_ID", defaultClientId);
        using IDisposable tokenFile = TestEnvironment.SetVariable("AZURE_FEDERATED_TOKEN_FILE", "/token.txt");

        _metadata.AccountName = AccountName;
        _metadata.ClientId = clientId;
        _metadata.EntraEndpoint = entraEndpoint is not null ? new Uri(entraEndpoint, UriKind.Absolute) : null;
        _metadata.UseManagedIdentity = true;

        AzureStorageAccountOptions options = new();
        _configure.Configure(options);

        Assert.Equal(AccountName, options.AccountName);
        Assert.Null(options.ConnectionString);
        Assert.Null(options.EndpointSuffix);
        Assert.NotNull(options.TokenCredential);

        AssertClientId(options.TokenCredential, clientId ?? defaultClientId);
    }

    private static void AssertClientId(WorkloadIdentityCredential tokenCredential, string? expected)
    {
        object? client = typeof(WorkloadIdentityCredential)
            .GetProperty("Client", BindingFlags.NonPublic | BindingFlags.Instance)?
            .GetValue(tokenCredential);

        Assert.NotNull(client);
        string? actual = typeof(ManagedIdentityCredential).Assembly
            .DefinedTypes
            .Single(x => x.FullName == "Azure.Identity.MsalClientBase`1")
            .MakeGenericType(typeof(IConfidentialClientApplication))
            .GetProperty("EntraClientId", BindingFlags.NonPublic | BindingFlags.Instance)?
            .GetValue(client) as string;

        Assert.Equal(expected, actual);
    }
}
