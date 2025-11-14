// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Keda.Scaler.DurableTask.AzureStorage.Interceptors;
using Keda.Scaler.DurableTask.AzureStorage.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Interceptors;

[TestClass]
public class ScalerMetadataInterceptorTest
{
    private readonly IScalerMetadataAccessor _metadataAccessor = Substitute.For<IScalerMetadataAccessor>();
    private readonly ScalerMetadataInterceptor _interceptor;

    public ScalerMetadataInterceptorTest()
        => _interceptor = new(_metadataAccessor);

    [TestMethod]
    public void GivenNullMetadataAccessor_WhenCreatingInterceptor_ThenThrowArgumentNullException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => new ScalerMetadataInterceptor(null!));

    [TestMethod]
    public Task GivenNullRequest_WhenProcessingUnaryServerRequest_ThenThrowArgumentNullException()
        => Assert.ThrowsExactlyAsync<ArgumentNullException>(() => _interceptor.UnaryServerHandler(null!, new MockServerCallContext(), CreateSimpleHandler<GetMetricsRequest, GetMetricsResponse>()));

    [TestMethod]
    public Task GivenNullContext_WhenProcessingUnaryServerRequest_ThenThrowArgumentNullException()
        => Assert.ThrowsExactlyAsync<ArgumentNullException>(() => _interceptor.UnaryServerHandler(new GetMetricsRequest(), null!, CreateSimpleHandler<GetMetricsRequest, GetMetricsResponse>()));

    [TestMethod]
    public Task GivenNullHandler_WhenProcessingUnaryServerRequest_ThenThrowArgumentNullException()
        => Assert.ThrowsExactlyAsync<ArgumentNullException>(() => _interceptor.UnaryServerHandler<GetMetricsRequest, GetMetricsResponse>(new GetMetricsRequest(), new MockServerCallContext(), null!));

    [TestMethod]
    public async ValueTask GivenGetMetricsRequest_WhenProcessingUnaryServerRequest_ThenCaptureMetadata()
    {
        const string TaskHubName = "UnitTest";

        GetMetricsRequest request = new()
        {
            ScaledObjectRef = new ScaledObjectRef
            {
                ScalerMetadata = { { "TaskHubName", TaskHubName } }
            },
        };

        using CancellationTokenSource cts = new();
        _ = await _interceptor.UnaryServerHandler(
            request,
            new MockServerCallContext(cts.Token),
            CreateSimpleHandler<GetMetricsRequest, GetMetricsResponse>());

        Assert.AreSame(request.ScaledObjectRef.ScalerMetadata, _metadataAccessor.ScalerMetadata);
    }

    [TestMethod]
    public async ValueTask GivenScaledObjectRef_WhenProcessingUnaryServerRequest_ThenCaptureMetadata()
    {
        const string TaskHubName = "UnitTest";

        ScaledObjectRef scaledObjectRef = new()
        {
            ScalerMetadata = { { "TaskHubName", TaskHubName } }
        };

        using CancellationTokenSource cts = new();
        _ = await _interceptor.UnaryServerHandler(
            scaledObjectRef,
            new MockServerCallContext(cts.Token),
            CreateSimpleHandler<ScaledObjectRef, GetMetricSpecResponse>());

        Assert.AreSame(scaledObjectRef.ScalerMetadata, _metadataAccessor.ScalerMetadata);
    }

    [TestMethod]
    public async ValueTask GivenUnknownRequest_WhenProcessingUnaryServerRequest_ThenSkipCapture()
    {
        using CancellationTokenSource cts = new();
        _ = await _interceptor.UnaryServerHandler(
            new object(),
            new MockServerCallContext(cts.Token),
            CreateSimpleHandler<object, object>());

        Assert.IsNull(_metadataAccessor.ScalerMetadata);
    }

    private static UnaryServerMethod<TRequest, TResponse> CreateSimpleHandler<TRequest, TResponse>()
        where TRequest : class
        where TResponse : class, new()
        => (request, context) => Task.FromResult(new TResponse());
}
