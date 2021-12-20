// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Auth;
using Moq;

namespace Keda.Scaler.DurableTask.AzureStorage.Cloud.Test
{
    [TestClass]
    public class TokenCredentialFactoryTest
    {
        [TestMethod]
        public void CtorExceptions()
            => Assert.ThrowsException<ArgumentNullException>(() => new TokenCredentialFactory(null!));

        [TestMethod]
        public async Task CreateAsync()
        {
            const string resource = "https://foo.bar.azure.com/";
            Uri authority = new Uri("https://login.foobaronline.com/", UriKind.Absolute);
            const string accessToken = "AAAA";

            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            Mock<AzureServiceTokenProvider> mock = new Mock<AzureServiceTokenProvider>(MockBehavior.Strict, "RunAs=App", authority.AbsoluteUri);
            mock.Setup(p => p.GetAccessTokenAsync(resource, null, tokenSource.Token)).ReturnsAsync(accessToken);

            TokenCredentialFactory factory = new TokenCredentialFactory(
                (connectionString, actualAuthority) =>
                {
                    Assert.AreEqual("RunAs=App", connectionString);
                    Assert.AreEqual(authority, actualAuthority);
                    return mock.Object;
                });

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => factory.CreateAsync(null!, authority, tokenSource.Token).AsTask()).ConfigureAwait(false);
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => factory.CreateAsync(resource, null!, tokenSource.Token).AsTask()).ConfigureAwait(false);

            TokenCredential actual = await factory.CreateAsync(resource, authority, tokenSource.Token).ConfigureAwait(false);
            Assert.AreEqual(accessToken, actual.Token);
        }
    }
}
