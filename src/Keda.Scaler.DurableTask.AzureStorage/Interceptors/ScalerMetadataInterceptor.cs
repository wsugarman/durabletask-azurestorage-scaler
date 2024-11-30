// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;
using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Options;
using Keda.Scaler.DurableTask.AzureStorage.Protobuf;
using Microsoft.Extensions.Configuration;

namespace Keda.Scaler.DurableTask.AzureStorage.Interceptors;

internal sealed class ScalerMetadataInterceptor(IScalerMetadataAccessor accessor) : Interceptor
{
    private readonly IScalerMetadataAccessor _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));

    private static readonly ValidateScalerMetadata MetadataValidator = new();

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

        _accessor.ScalerMetadata = ParseScalerMetadata(mapField);
        return continuation(request, context);
    }

    private static ScalerMetadata ParseScalerMetadata(MapField<string, string> mapField)
    {
        ScalerMetadata metadata = new();
        mapField.ToConfiguration().Bind(metadata);

        ValidateOptionsResult result = MetadataValidator.Validate(null, metadata);
        if (result.Failed)
            throw new ValidationException(result.FailureMessage);

        return metadata;
    }
}
