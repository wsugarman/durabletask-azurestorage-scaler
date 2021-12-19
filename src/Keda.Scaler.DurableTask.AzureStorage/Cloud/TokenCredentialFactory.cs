// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.WindowsAzure.Storage.Auth;

namespace Keda.Scaler.DurableTask.AzureStorage.Cloud
{
    internal sealed class TokenCredentialFactory
    {
        private const string StorageAccountResource = "https://storage.azure.com/";

        private readonly Func<string, Uri, AzureServiceTokenProvider> _tokenProviderFactory;

        public TokenCredentialFactory()
            : this((s, a) => new AzureServiceTokenProvider(s, a.AbsoluteUri))
        { }

        internal TokenCredentialFactory(Func<string, Uri, AzureServiceTokenProvider> tokenProviderFactory)
            => _tokenProviderFactory = tokenProviderFactory ?? throw new ArgumentNullException(nameof(tokenProviderFactory));

        public async ValueTask<TokenCredential> CreateAsync(Uri authorityHost, CancellationToken cancellationToken = default)
        {
            if (authorityHost is null)
                throw new ArgumentNullException(nameof(authorityHost));

            // TODO: Implement token renewal if necessary, but lifetime should be valid for duration of request
            AzureServiceTokenProvider tokenProvider = _tokenProviderFactory("RunAs=App", authorityHost);
            return new TokenCredential(await tokenProvider.GetAccessTokenAsync(StorageAccountResource, cancellationToken: cancellationToken).ConfigureAwait(false));
        }
    }
}
