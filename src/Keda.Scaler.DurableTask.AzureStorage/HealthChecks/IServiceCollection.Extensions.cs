// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.HealthChecks;

internal static class IServiceCollectionExtensions
{
    public static IServiceCollection AddKubernetesHealthCheck(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _ = services
            .AddSingleton<IValidateOptions<HealthCheckOptions>, ValidateHealthCheckOptions>()
            .AddOptions<HealthCheckOptions>()
            .BindConfiguration(HealthCheckOptions.DefaultKey);

        _ = services
            .AddGrpcHealthChecks()
            .AddCheck<TlsHealthCheck>("TLS");

        return services;
    }
}
