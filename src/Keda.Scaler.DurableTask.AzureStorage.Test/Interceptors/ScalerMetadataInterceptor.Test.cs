// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Keda.Scaler.DurableTask.AzureStorage.Interceptors;
using Keda.Scaler.DurableTask.AzureStorage.Metadata;
using NSubstitute;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Interceptors;

public class ScalerMetadataInterceptorTest
{
    private readonly IScalerMetadataAccessor _metadataAccessor = Substitute.For<IScalerMetadataAccessor>();
    private readonly ScalerMetadataInterceptor _interceptor;

    public ScalerMetadataInterceptorTest()
        => _interceptor = new(_metadataAccessor);

    [Fact]
    public void GivenNullMetadataAccessor_WhenCreatingInterceptor_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new ScalerMetadataInterceptor(null!));

    [Fact]
    public Task GivenNullRequest_WhenProcessingUnaryServerRequest_ThenThrowArgumentNullException()
        => Assert.ThrowsAsync<ArgumentNullException>(() => _interceptor.UnaryServerHandler(null!, new MockServerCallContext(), CreateSimpleHandler<GetMetricsRequest, GetMetricsResponse>()));

    [Fact]
    public Task GivenNullContext_WhenProcessingUnaryServerRequest_ThenThrowArgumentNullException()
        => Assert.ThrowsAsync<ArgumentNullException>(() => _interceptor.UnaryServerHandler(new GetMetricsRequest(), null!, CreateSimpleHandler<GetMetricsRequest, GetMetricsResponse>()));

    [Fact]
    public Task GivenNullHandler_WhenProcessingUnaryServerRequest_ThenThrowArgumentNullException()
        => Assert.ThrowsAsync<ArgumentNullException>(() => _interceptor.UnaryServerHandler<GetMetricsRequest, GetMetricsResponse>(new GetMetricsRequest(), new MockServerCallContext(), null!));

    [Fact]
    public async Task GivenGetMetricsRequest_WhenProcessingUnaryServerRequest_ThenCaptureMetadata()
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

        Assert.Same(request.ScaledObjectRef.ScalerMetadata, _metadataAccessor.ScalerMetadata);
    }

    [Fact]
    public async Task GivenScaledObjectRef_WhenProcessingUnaryServerRequest_ThenCaptureMetadata()
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

        Assert.Same(scaledObjectRef.ScalerMetadata, _metadataAccessor.ScalerMetadata);
    }

    [Fact]
    public async Task GivenUnknownRequest_WhenProcessingUnaryServerRequest_ThenSkipCapture()
    {
        using CancellationTokenSource cts = new();
        _ = await _interceptor.UnaryServerHandler(
            new object(),
            new MockServerCallContext(cts.Token),
            CreateSimpleHandler<object, object>());

        Assert.Null(_metadataAccessor.ScalerMetadata);
    }

    private static UnaryServerMethod<TRequest, TResponse> CreateSimpleHandler<TRequest, TResponse>()
        where TRequest : class
        where TResponse : class, new()
        => (request, context) => Task.FromResult(new TResponse());
}
