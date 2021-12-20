// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Auth;

namespace Keda.Scaler.DurableTask.AzureStorage.Cloud
{
    internal interface ITokenCredentialFactory
    {
        ValueTask<TokenCredential> CreateAsync(string resource, Uri authorityHost, CancellationToken cancellationToken = default);
    }
}
