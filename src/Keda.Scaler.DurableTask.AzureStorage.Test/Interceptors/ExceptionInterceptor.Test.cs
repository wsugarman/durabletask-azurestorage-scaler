// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Keda.Scaler.DurableTask.AzureStorage.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Rest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Interceptors;

[TestClass]
public class ExceptionInterceptorTest
{
    private readonly ExceptionInterceptor _interceptor = new ExceptionInterceptor(NullLoggerFactory.Instance);

    [TestMethod]
    public async Task UnaryServerHandler()
    {
        Request request = new Request();
        Response response = new Response();
        UnaryServerMethod<Request, Response> continuation = CreateContinuation(response);
        MockServerCallContext context = new MockServerCallContext(default);

        // Null input
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _interceptor.UnaryServerHandler(null!, context, continuation)).ConfigureAwait(false);
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _interceptor.UnaryServerHandler(request, null!, continuation)).ConfigureAwait(false);
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _interceptor.UnaryServerHandler<Request, Response>(request, context, null!)).ConfigureAwait(false);

        // Success
        Assert.AreSame(response, await _interceptor.UnaryServerHandler(request, context, continuation).ConfigureAwait(false));

        RpcException actual;

        // AggregateException
        actual = await Assert.ThrowsExceptionAsync<RpcException>(() => _interceptor.UnaryServerHandler(
            request,
            context,
            CreateErrorContinuation(
                new AggregateException(new ValidationException[]
                {
                    new ValidationException("One"),
                    new ValidationException("Two"),
                    new ValidationException("Three"),
                })))).ConfigureAwait(false);
        Assert.AreEqual(StatusCodes.Status400BadRequest, context.HttpContext.Response.StatusCode);
        Assert.AreEqual(StatusCode.InvalidArgument, actual.StatusCode);

        // ValidationException
        actual = await Assert.ThrowsExceptionAsync<RpcException>(() => _interceptor.UnaryServerHandler(
            request,
            context,
            CreateErrorContinuation(new ValidationException("Can't be null")))).ConfigureAwait(false);
        Assert.AreEqual(StatusCodes.Status400BadRequest, context.HttpContext.Response.StatusCode);
        Assert.AreEqual(StatusCode.InvalidArgument, actual.StatusCode);

        // Exception
        actual = await Assert.ThrowsExceptionAsync<RpcException>(() => _interceptor.UnaryServerHandler(
            request,
            context,
            CreateErrorContinuation(new IOException()))).ConfigureAwait(false);
        Assert.AreEqual(StatusCodes.Status500InternalServerError, context.HttpContext.Response.StatusCode);
        Assert.AreEqual(StatusCode.Internal, actual.StatusCode);

        // OperationCanceledException
        using CancellationTokenSource tokenSource = new CancellationTokenSource();

        actual = await Assert.ThrowsExceptionAsync<RpcException>(() => _interceptor.UnaryServerHandler(
            request,
            context,
            CreateErrorContinuation(new OperationCanceledException(tokenSource.Token)))).ConfigureAwait(false);
        Assert.AreEqual(StatusCodes.Status500InternalServerError, context.HttpContext.Response.StatusCode);
        Assert.AreEqual(StatusCode.Internal, actual.StatusCode); // Different token from context

        context = new MockServerCallContext(tokenSource.Token);
        actual = await Assert.ThrowsExceptionAsync<RpcException>(() => _interceptor.UnaryServerHandler(
            request,
            context,
            CreateErrorContinuation(new OperationCanceledException(tokenSource.Token)))).ConfigureAwait(false);
        Assert.AreEqual(StatusCodes.Status400BadRequest, context.HttpContext.Response.StatusCode);
        Assert.AreEqual(StatusCode.Cancelled, actual.StatusCode);
    }

    private static UnaryServerMethod<Request, Response> CreateContinuation(Response response)
        => (Request r, ServerCallContext c) => Task.FromResult(response);

    private static UnaryServerMethod<Request, Response> CreateErrorContinuation(Exception e)
        => (Request r, ServerCallContext c) => Task.FromException<Response>(e);

    private sealed class Request
    { }

    private sealed class Response
    { }
}
