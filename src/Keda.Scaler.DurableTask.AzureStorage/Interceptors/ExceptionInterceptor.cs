// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Keda.Scaler.DurableTask.AzureStorage.Interceptors;

internal sealed partial class ExceptionInterceptor(ILoggerFactory loggerFactory) : Interceptor
{
    private readonly ILogger _logger = loggerFactory?.CreateLogger(LogCategories.Default) ?? throw new ArgumentNullException(nameof(loggerFactory));

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(continuation);

        try
        {
            return await continuation(request, context);
        }
        catch (ValidationException v)
        {
            LogInvalidInput(_logger, v);
            throw new RpcException(new Status(StatusCode.InvalidArgument, v.Message));
        }
        catch (OperationCanceledException oce) when (context.CancellationToken.IsCancellationRequested)
        {
            LogRequestCancellation(_logger, oce);
            throw new RpcException(Status.DefaultCancelled);
        }
        catch (Exception e)
        {
            LogUnhandledException(_logger, e);
            throw new RpcException(new Status(StatusCode.Internal, SR.InternalServerError));
        }
    }

    [LoggerMessage(
        Level = LogLevel.Critical,
        Message = "Caught unhandled exception!")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Request contains invalid input.")]
    private static partial void LogInvalidInput(ILogger logger, Exception exception);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "RPC operation canceled.")]
    private static partial void LogRequestCancellation(ILogger logger, OperationCanceledException exception);
}
