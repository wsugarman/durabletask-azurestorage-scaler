// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Keda.Scaler.DurableTask.AzureStorage.Metadata;

namespace Keda.Scaler.DurableTask.AzureStorage.Interceptors;

internal sealed class ScalerMetadataInterceptor(ScalerMetadata metadata) : Interceptor
{
    private readonly ScalerMetadata _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));

    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(continuation);

        _metadata.Values = request switch
        {
            GetMetricsRequest r => r.ScaledObjectRef.ScalerMetadata,
            ScaledObjectRef r => r.ScalerMetadata,
            _ => null,
        };

        return continuation(request, context);
    }
}
