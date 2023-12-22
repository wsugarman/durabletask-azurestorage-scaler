// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.HealthChecks;

internal static class IServiceCollectionExtensions
{
    public static IServiceCollection AddKubernetesHealthCheck(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        _ = services
            .AddSingleton<IValidateOptions<HealthCheckOptions>, ValidateHealthCheckOptions>()
            .AddOptions<HealthCheckOptions>()
            .BindConfiguration(HealthCheckOptions.DefaultKey);

        // Avoid registering the health check if it is not needed to avoid
        // having the health check hosted service work in the background
        if (configuration.EnforceTls())
        {
            _ = services
                .AddGrpcHealthChecks()
                .AddCheck<TlsHealthCheck>("TLS");
        }

        return services;
    }
}
