// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Keda.Scaler.DurableTask.AzureStorage.Test;

internal sealed class MockServerCallContext : ServerCallContext
{
    protected override CancellationToken CancellationTokenCore { get; }

    public HttpContext HttpContext => (HttpContext)UserState["__HttpContext"];

    public MockServerCallContext(CancellationToken cancellationToken = default)
    {
        CancellationTokenCore = cancellationToken;

        HttpRequest httpRequest = Mock.Of<HttpRequest>();
        HttpResponse httpResponse = Mock.Of<HttpResponse>();
        UserState["__HttpContext"] = Mock.Of<HttpContext>(
            c => c.Request == httpRequest && c.Response == httpResponse,
            MockBehavior.Strict);
    }

    #region Not Implmented

    protected override string MethodCore => throw new NotImplementedException();

    protected override string HostCore => throw new NotImplementedException();

    protected override string PeerCore => throw new NotImplementedException();

    protected override DateTime DeadlineCore => throw new NotImplementedException();

    protected override Metadata RequestHeadersCore => throw new NotImplementedException();

    protected override Metadata ResponseTrailersCore => throw new NotImplementedException();

    protected override Status StatusCore { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    protected override WriteOptions WriteOptionsCore { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    protected override AuthContext AuthContextCore => throw new NotImplementedException();

    protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) => throw new NotImplementedException();

    protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => throw new NotImplementedException();

    #endregion
}
