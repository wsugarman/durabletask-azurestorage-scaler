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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Interceptors;

[TestClass]
public sealed class ExceptionInterceptorTest
{
    private readonly ExceptionInterceptor _interceptor = new(NullLoggerFactory.Instance);

    [TestMethod]
    public void GivenNullLoggerFactory_WhenCreatingInterceptor_ThenThrowArgumentNullException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => new ExceptionInterceptor(null!));

    [TestMethod]
    public void GivenNullLogger_WhenCreatingInterceptor_ThenThrowArgumentNullException()
    {
        ILoggerFactory factory = Substitute.For<ILoggerFactory>();
        _ = factory.CreateLogger(default!).ReturnsForAnyArgs((ILogger)null!);

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ExceptionInterceptor(factory));
    }

    [TestMethod]
    [DataRow(StatusCode.InvalidArgument, typeof(ValidationException), false)]
    [DataRow(StatusCode.InvalidArgument, typeof(ValidationException), true)]
    [DataRow(StatusCode.Cancelled, typeof(TaskCanceledException), true)]
    [DataRow(StatusCode.Cancelled, typeof(OperationCanceledException), true)]
    [DataRow(StatusCode.Internal, typeof(OperationCanceledException), false)]
    [DataRow(StatusCode.Internal, typeof(NullReferenceException), false)]
    [DataRow(StatusCode.Internal, typeof(OutOfMemoryException), true)]
    public async ValueTask GivenCaughtException_WhenHandlingUnaryRequest_ThenThrowRpcException(StatusCode expected, Type exceptionType, bool canceled)
    {
        using CancellationTokenSource tokenSource = new();
        if (canceled)
            await tokenSource.CancelAsync();

        Request request = new();
        MockServerCallContext context = new(tokenSource.Token);

        RpcException actual = await Assert.ThrowsExactlyAsync<RpcException>(() => _interceptor.UnaryServerHandler(request, context, GetContinuation()));
        Assert.AreEqual(expected, actual.Status.StatusCode);

        UnaryServerMethod<Request, Response> GetContinuation()
            => (Request r, ServerCallContext c) => Task.FromException<Response>((Exception)Activator.CreateInstance(exceptionType)!);
    }

    [TestMethod]
    public async ValueTask GivenNoException_WhenHandlingUnaryRequest_ThenReturnContinuation()
    {
        Request request = new();
        Response response = new();
        MockServerCallContext context = new();

        Assert.AreSame(response, await _interceptor.UnaryServerHandler(request, context, GetContinuation()));

        UnaryServerMethod<Request, Response> GetContinuation()
            => (Request r, ServerCallContext c) => Task.FromResult(response);
    }

    private sealed class Request
    { }

    private sealed class Response
    { }
}
