// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Reflection;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Data.Tables;
using Keda.Scaler.DurableTask.AzureStorage.Clients;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Clients;

[TestClass]
public class TableServiceClientFactoryTest : AzureStorageAccountClientFactoryTest<TableServiceClient>
{
    protected override AzureStorageAccountClientFactory<TableServiceClient> GetFactory()
        => new TableServiceClientFactory();

    protected override void ValidateAccountName(TableServiceClient actual, string accountName, string endpointSuffix)
        => Validate(actual, accountName, AzureStorageServiceUri.Create(accountName, AzureStorageService.Table, endpointSuffix));

    protected override void ValidateEmulator(TableServiceClient actual)
        => Validate(actual, "devstoreaccount1", new Uri("http://127.0.0.1:10002/devstoreaccount1", UriKind.Absolute));

    protected override void AssertTokenCredential<T>(TableServiceClient client)
    {
        Type cacheType = typeof(BearerTokenAuthenticationPolicy)
            .GetNestedTypes(BindingFlags.NonPublic)
            .Single(x => x.FullName == "Azure.Core.Pipeline.BearerTokenAuthenticationPolicy+AccessTokenCache");

        HttpPipeline pipeline = Assert.IsInstanceOfType<HttpPipeline>(typeof(TableServiceClient)
            .GetField("_pipeline", BindingFlags.NonPublic | BindingFlags.Instance)?
            .GetValue(client));
        ReadOnlyMemory<HttpPipelinePolicy> pipelineMemory = Assert.IsInstanceOfType<ReadOnlyMemory<HttpPipelinePolicy>>(typeof(HttpPipeline)
            .GetField("_pipeline", BindingFlags.NonPublic | BindingFlags.Instance)?
            .GetValue(pipeline));
        object? tokenCache = typeof(BearerTokenAuthenticationPolicy)
            .GetField("_accessTokenCache", BindingFlags.NonPublic | BindingFlags.Instance)?
            .GetValue(pipelineMemory
                .ToArray()
                .Single(x => x.GetType().IsAssignableTo(typeof(BearerTokenAuthenticationPolicy))));

        Assert.IsNotNull(tokenCache);
        TokenCredential? tokenCredential = cacheType
            .GetField("_credential", BindingFlags.NonPublic | BindingFlags.Instance)?
            .GetValue(tokenCache) as TokenCredential;

        _ = Assert.IsInstanceOfType<T>(tokenCredential);
    }

    private static void Validate(TableServiceClient actual, string accountName, Uri serviceUrl)
    {
        Assert.AreEqual(accountName, actual?.AccountName);
        Assert.AreEqual(serviceUrl, actual?.Uri);
    }
}
