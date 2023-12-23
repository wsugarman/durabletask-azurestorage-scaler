// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.HealthChecks;

[ExcludeFromCodeCoverage(Justification = "Tested via Helm integration tests.")]
internal static class WebApplicationExtensions
{
    public static WebApplication ConfigureKubernetesHealthCheck(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        _ = app.MapWhen(
            context => context
                .RequestServices
                .GetRequiredService<IOptions<HealthCheckOptions>>()
                .Value
                .IsHealthCheckRequest(context),
            builder => builder
                .UseRouting()
                .UseEndpoints(e => e.MapGrpcHealthChecksService()));

        return app;
    }
}
