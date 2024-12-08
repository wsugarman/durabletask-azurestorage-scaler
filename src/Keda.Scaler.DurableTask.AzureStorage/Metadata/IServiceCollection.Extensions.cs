// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.Metadata;

internal static class IServiceCollectionExtensions
{
    public static IServiceCollection AddScalerMetadata(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services
            .AddSingleton<IScalerMetadataAccessor, ScalerMetadataAccessor>()
            .AddSingleton<IConfigureOptions<ScalerOptions>, ConfigureScalerOptions>()
            .AddSingleton<IValidateOptions<ScalerOptions>, ValidateScalerOptions>();
    }
}
