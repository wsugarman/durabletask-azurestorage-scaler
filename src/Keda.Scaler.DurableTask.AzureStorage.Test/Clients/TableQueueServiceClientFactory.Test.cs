// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Azure.Core;
using System.Reflection;
using Azure.Data.Tables;
using Keda.Scaler.DurableTask.AzureStorage.Clients;
using Xunit;
using Azure.Core.Pipeline;
using System.Linq;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Clients;

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

        HttpPipeline pipeline = Assert.IsType<HttpPipeline>(typeof(TableServiceClient)
            .GetField("_pipeline", BindingFlags.NonPublic | BindingFlags.Instance)?
            .GetValue(client));
        ReadOnlyMemory<HttpPipelinePolicy> pipelineMemory = Assert.IsType<ReadOnlyMemory<HttpPipelinePolicy>>(typeof(HttpPipeline)
            .GetField("_pipeline", BindingFlags.NonPublic | BindingFlags.Instance)?
            .GetValue(pipeline));
        object? tokenCache = typeof(BearerTokenAuthenticationPolicy)
            .GetField("_accessTokenCache", BindingFlags.NonPublic | BindingFlags.Instance)?
            .GetValue(pipelineMemory
                .ToArray()
                .Single(x => x.GetType().IsAssignableTo(typeof(BearerTokenAuthenticationPolicy))));

        Assert.NotNull(tokenCache);
        TokenCredential? tokenCredential = cacheType
            .GetField("_credential", BindingFlags.NonPublic | BindingFlags.Instance)?
            .GetValue(tokenCache) as TokenCredential;

        _ = Assert.IsType<T>(tokenCredential);
    }

    private static void Validate(TableServiceClient actual, string accountName, Uri serviceUrl)
    {
        Assert.Equal(accountName, actual?.AccountName);
        Assert.Equal(serviceUrl, actual?.Uri);
    }
}
