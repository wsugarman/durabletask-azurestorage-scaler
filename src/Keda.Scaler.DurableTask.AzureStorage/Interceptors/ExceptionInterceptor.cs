// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Keda.Scaler.DurableTask.AzureStorage.Interceptors;

internal sealed class ExceptionInterceptor : Interceptor
{
    private readonly ILogger _logger;

    public ExceptionInterceptor(ILoggerFactory loggerFactory)
        => _logger = loggerFactory?.CreateLogger(Diagnostics.LoggerCategory) ?? throw new ArgumentNullException(nameof(loggerFactory));

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (context is null)
            throw new ArgumentNullException(nameof(context));

        if (continuation is null)
            throw new ArgumentNullException(nameof(continuation));

        try
        {
            return await continuation(request, context).ConfigureAwait(false);
        }
        catch (AggregateException ae) when (ae.InnerExceptions is not null && ae.InnerExceptions.All(i => i is ValidationException))
        {
            _logger.ReceivedInvalidInput(ae);

            // Could also aggregate messages together
            context.GetHttpContext().Response.StatusCode = StatusCodes.Status400BadRequest;
            throw new RpcException(new Status(StatusCode.InvalidArgument, ae.InnerExceptions.First().Message));
        }
        catch (ValidationException v)
        {
            _logger.ReceivedInvalidInput(v);

            context.GetHttpContext().Response.StatusCode = StatusCodes.Status400BadRequest;
            throw new RpcException(new Status(StatusCode.InvalidArgument, v.Message));
        }
        catch (OperationCanceledException oce) when (oce.CancellationToken == context.CancellationToken)
        {
            _logger.DetectedRequestCancellation(oce);

            // Response code is not seen by user, but could use 499 like nginx
            context.GetHttpContext().Response.StatusCode = StatusCodes.Status400BadRequest;
            throw new RpcException(Status.DefaultCancelled);
        }
        catch (Exception e)
        {
            _logger.CaughtUnhandledException(e);

            context.GetHttpContext().Response.StatusCode = StatusCodes.Status500InternalServerError;
            throw new RpcException(new Status(StatusCode.Internal, SR.InternalErrorMessage));
        }
    }
}
