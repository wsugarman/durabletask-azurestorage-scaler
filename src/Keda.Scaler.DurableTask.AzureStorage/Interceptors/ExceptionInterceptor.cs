// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Keda.Scaler.DurableTask.AzureStorage.Interceptors;

internal sealed class ExceptionInterceptor(ILoggerFactory loggerFactory) : Interceptor
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
            return await continuation(request, context).ConfigureAwait(false);
        }
        catch (ValidationException v)
        {
            _logger.ReceivedInvalidInput(v);
            throw new RpcException(new Status(StatusCode.InvalidArgument, v.Message));
        }
        catch (OperationCanceledException oce) when (context.CancellationToken.IsCancellationRequested)
        {
            _logger.DetectedRequestCancellation(oce);
            throw new RpcException(Status.DefaultCancelled);
        }
        catch (Exception e)
        {
            _logger.CaughtUnhandledException(e);
            throw new RpcException(new Status(StatusCode.Internal, SR.InternalServerError));
        }
    }
}
