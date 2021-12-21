// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.WindowsAzure.Storage.Auth;

namespace Keda.Scaler.DurableTask.AzureStorage.Cloud
{
    internal sealed class TokenCredentialFactory : ITokenCredentialFactory
    {
        private readonly Func<string, Uri, AzureServiceTokenProvider> _tokenProviderFactory;

        [ExcludeFromCodeCoverage]
        public TokenCredentialFactory()
            : this(CreateAzureServiceTokenProvider)
        { }

        internal TokenCredentialFactory(Func<string, Uri, AzureServiceTokenProvider> tokenProviderFactory)
            => _tokenProviderFactory = tokenProviderFactory ?? throw new ArgumentNullException(nameof(tokenProviderFactory));

        public async ValueTask<TokenCredential> CreateAsync(string resource, Uri authorityHost, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(resource))
                throw new ArgumentNullException(nameof(resource));

            if (authorityHost is null)
                throw new ArgumentNullException(nameof(authorityHost));

            // TODO: Implement token renewal if necessary, but lifetime should be valid for duration of request
            AzureServiceTokenProvider tokenProvider = _tokenProviderFactory("RunAs=App", authorityHost);
            return new TokenCredential(await tokenProvider.GetAccessTokenAsync(resource, cancellationToken: cancellationToken).ConfigureAwait(false));
        }

        [ExcludeFromCodeCoverage]
        private static AzureServiceTokenProvider CreateAzureServiceTokenProvider(string connectionString, Uri authorityHost)
            => new AzureServiceTokenProvider(connectionString, authorityHost.AbsoluteUri);
    }
}
