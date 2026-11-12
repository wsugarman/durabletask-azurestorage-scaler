// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Reflection;
using Azure.Identity;
using Keda.Scaler.DurableTask.AzureStorage.Clients;
using Keda.Scaler.DurableTask.AzureStorage.Metadata;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Clients;

[TestClass]
[DoNotParallelize]
public class ConfigureAzureStorageAccountOptionsTest
{
    private readonly ScalerOptions _scalerOptions = new();
    private readonly ConfigureAzureStorageAccountOptions _configure;

    public ConfigureAzureStorageAccountOptionsTest()
    {
        IOptionsSnapshot<ScalerOptions> snapshot = Substitute.For<IOptionsSnapshot<ScalerOptions>>();
        _ = snapshot.Get(default).Returns(_scalerOptions);
        _configure = new(snapshot);
    }

    [TestMethod]
    public void GivenNullOptionsSnapshot_WhenCreatingConfigure_ThenThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ConfigureAzureStorageAccountOptions(null!));

        IOptionsSnapshot<ScalerOptions> nullSnapshot = Substitute.For<IOptionsSnapshot<ScalerOptions>>();
        _ = nullSnapshot.Get(default).Returns(default(ScalerOptions));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ConfigureAzureStorageAccountOptions(nullSnapshot));
    }

    [TestMethod]
    [DataRow("Key=1", "Key=1", null)]
    [DataRow("Key=1", null, "Key=1")]
    public void GivenMetadataWithConnectionString_WhenConfiguringOptions_ThenConfigureConnectionString(string expected, string? connection, string? connectionFromEnv)
    {
        _scalerOptions.Connection = connection;
        _scalerOptions.ConnectionFromEnv = connectionFromEnv;

        AzureStorageAccountOptions options = new();
        _configure.Configure(options);

        Assert.IsNull(options.AccountName);
        Assert.AreEqual(expected, options.ConnectionString);
        Assert.IsNull(options.EndpointSuffix);
        Assert.IsNull(options.TokenCredential);
    }

    [TestMethod]
    public void GivenNoConnectionMetadata_WhenConfiguringOptions_ThenIgnoreScalerEnvironment()
    {
        using IDisposable env = TestEnvironment.SetVariable("AzureWebJobsStorage", "Key=1");

        AzureStorageAccountOptions options = new();
        _configure.Configure(options);

        Assert.IsNull(options.AccountName);
        Assert.IsNull(options.ConnectionString);
    }

    [TestMethod]
    [DataRow("Key=1", "Key=2", null)]
    [DataRow("Key=1", null, "Key=2")]
    [DataRow("Key=1", null, null)]
    public void GivenMetadataWithAccount_WhenConfiguringOptions_ThenConfigureUriConnection(string accountName, string? connection, string? connectionFromEnv)
    {
        _scalerOptions.AccountName = accountName;
        _scalerOptions.Connection = connection;
        _scalerOptions.ConnectionFromEnv = connectionFromEnv;

        AzureStorageAccountOptions options = new();
        _configure.Configure(options);

        Assert.AreEqual(accountName, options.AccountName);
        Assert.IsNull(options.ConnectionString);
        Assert.AreEqual(AzureStorageServiceUri.PublicSuffix, options.EndpointSuffix);
        Assert.IsNull(options.TokenCredential);
    }

    [TestMethod]
    [DataRow(AzureStorageServiceUri.PublicSuffix, null, null)]
    [DataRow(AzureStorageServiceUri.PublicSuffix, nameof(CloudEnvironment.AzurePublicCloud), null)]
    [DataRow(AzureStorageServiceUri.USGovernmentSuffix, "AZUREUSGOVERNMENTCLOUD", null)]
    [DataRow("unit.test.cloud", "priVATE", "unit.test.cloud")]
    public void GivenMetadataWithAccount_WhenConfiguringOptions_ThenConfigureEndpointBasedOnCloud(string? expected, string? cloud, string? endpointSuffix)
    {
        const string AccountName = "unittest";

        _scalerOptions.AccountName = AccountName;
        _scalerOptions.Cloud = cloud;
        _scalerOptions.EntraEndpoint = new Uri("https://entra.unit.test", UriKind.Absolute);
        _scalerOptions.EndpointSuffix = endpointSuffix;

        AzureStorageAccountOptions options = new();
        _configure.Configure(options);

        Assert.AreEqual(AccountName, options.AccountName);
        Assert.IsNull(options.ConnectionString);
        Assert.AreEqual(expected, options.EndpointSuffix);
        Assert.IsNull(options.TokenCredential);
    }

    [TestMethod]
    [DataRow(null, null)]
    [DataRow("https://entra.unit.test", "12345")]
    public void GivenMetadataWithAccount_WhenConfiguringOptions_ThenConfigureTokenCredential(string? entraEndpoint, string? clientId)
    {
        const string AccountName = "unittest";
        string defaultClientId = Guid.NewGuid().ToString();

        using IDisposable tenant = TestEnvironment.SetVariable("AZURE_TENANT_ID", Guid.NewGuid().ToString());
        using IDisposable client = TestEnvironment.SetVariable("AZURE_CLIENT_ID", defaultClientId);
        using IDisposable tokenFile = TestEnvironment.SetVariable("AZURE_FEDERATED_TOKEN_FILE", "/token.txt");

        _scalerOptions.AccountName = AccountName;
        _scalerOptions.ClientId = clientId;
        _scalerOptions.EntraEndpoint = entraEndpoint is not null ? new Uri(entraEndpoint, UriKind.Absolute) : null;
        _scalerOptions.UseManagedIdentity = true;

        AzureStorageAccountOptions options = new();
        _configure.Configure(options);

        Assert.AreEqual(AccountName, options.AccountName);
        Assert.IsNull(options.ConnectionString);
        Assert.AreEqual(AzureStorageServiceUri.PublicSuffix, options.EndpointSuffix);
        Assert.IsNotNull(options.TokenCredential);

        AssertClientId(options.TokenCredential, clientId ?? defaultClientId);
    }

    private static void AssertClientId(WorkloadIdentityCredential tokenCredential, string? expected)
    {
        object? client = typeof(WorkloadIdentityCredential)
            .GetProperty("Client", BindingFlags.NonPublic | BindingFlags.Instance)?
            .GetValue(tokenCredential);

        Assert.IsNotNull(client);
        string? actual = typeof(WorkloadIdentityCredential).Assembly
            .DefinedTypes
            .Single(x => x.FullName == "Azure.Identity.MsalClientBase`1")
            .MakeGenericType(typeof(IConfidentialClientApplication))
            .GetProperty("ClientId", BindingFlags.NonPublic | BindingFlags.Instance)?
            .GetValue(client) as string;

        Assert.AreEqual(expected, actual);
    }
}
