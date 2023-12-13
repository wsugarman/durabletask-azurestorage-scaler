// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Keda.Scaler.DurableTask.AzureStorage.Interceptors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Interceptors;

public class ExceptionInterceptorTest
{
    private readonly ExceptionInterceptor _interceptor = new(NullLoggerFactory.Instance);

    [Fact]
    public void GivenNullLoggerFactory_WhenCreatingInterceptor_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new ExceptionInterceptor(null!));

    [Fact]
    public void GivenNullLogger_WhenCreatingInterceptor_ThenThrowArgumentNullException()
    {
        ILoggerFactory factory = Substitute.For<ILoggerFactory>();
        _ = factory.CreateLogger(default!).ReturnsForAnyArgs((ILogger)null!);

        _ = Assert.Throws<ArgumentNullException>(() => new ExceptionInterceptor(factory));
    }

    [Theory]
    [InlineData(StatusCode.InvalidArgument, typeof(ValidationException), false)]
    [InlineData(StatusCode.InvalidArgument, typeof(ValidationException), true)]
    [InlineData(StatusCode.Cancelled, typeof(TaskCanceledException), true)]
    [InlineData(StatusCode.Cancelled, typeof(OperationCanceledException), true)]
    [InlineData(StatusCode.Internal, typeof(OperationCanceledException), false)]
    [InlineData(StatusCode.Internal, typeof(NullReferenceException), false)]
    [InlineData(StatusCode.Internal, typeof(OutOfMemoryException), true)]
    public async Task GivenCaughtException_WhenHandlingUnaryRequest_ThenThrowRpcException(StatusCode expected, Type exceptionType, bool canceled)
    {
        using CancellationTokenSource tokenSource = new();
        if (canceled)
            await tokenSource.CancelAsync();

        Request request = new();
        MockServerCallContext context = new(tokenSource.Token);

        RpcException actual = await Assert.ThrowsAsync<RpcException>(() => _interceptor.UnaryServerHandler(request, context, GetContinuation()));
        Assert.Equal(expected, actual.Status.StatusCode);

        UnaryServerMethod<Request, Response> GetContinuation()
            => (Request r, ServerCallContext c) => Task.FromException<Response>((Exception)Activator.CreateInstance(exceptionType)!);
    }

    [Fact]
    public async Task GivenNoException_WhenHandlingUnaryRequest_ThenReturnContinuation()
    {
        Request request = new();
        Response response = new();
        MockServerCallContext context = new();

        Assert.Same(response, await _interceptor.UnaryServerHandler(request, context, GetContinuation()));

        UnaryServerMethod<Request, Response> GetContinuation()
            => (Request r, ServerCallContext c) => Task.FromResult(response);
    }

    private sealed class Request
    { }

    private sealed class Response
    { }
}
