// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Keda.Scaler.DurableTask.AzureStorage.Metadata;

namespace Keda.Scaler.DurableTask.AzureStorage.Interceptors;

internal sealed class ScalerMetadataInterceptor(IScalerMetadataAccessor accessor) : Interceptor
{
    private readonly IScalerMetadataAccessor _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));

    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(continuation);

        _accessor.ScalerMetadata = request switch
        {
            GetMetricsRequest r => r.ScaledObjectRef.ScalerMetadata,
            ScaledObjectRef r => r.ScalerMetadata,
            _ => null,
        };

        return continuation(request, context);
    }
}
