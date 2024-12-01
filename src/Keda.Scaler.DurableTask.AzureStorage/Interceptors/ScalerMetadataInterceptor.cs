// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Google.Protobuf.Collections;
using Keda.Scaler.DurableTask.AzureStorage.Protobuf;
using Microsoft.Extensions.Configuration;

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

        MapField<string, string> mapField = request switch
        {
            GetMetricsRequest r => r.ScaledObjectRef.ScalerMetadata,
            ScaledObjectRef r => r.ScalerMetadata,
            _ => throw new ArgumentException(SR.InvalidRequestType, nameof(request))
        };

        ScalerMetadata metadata = new();
        mapField.ToConfiguration().Bind(metadata);

        _accessor.ScalerMetadata = metadata;
        return continuation(request, context);
    }
}
