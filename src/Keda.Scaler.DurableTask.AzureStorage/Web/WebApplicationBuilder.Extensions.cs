// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using Keda.Scaler.DurableTask.AzureStorage.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Keda.Scaler.DurableTask.AzureStorage.Web;

internal static class WebApplicationBuilderExtensions
{
    [ExcludeFromCodeCoverage(Justification = "Tested via Helm integration tests.")]
    public static WebApplicationBuilder ConfigureKestrelTls(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        _ = builder.WebHost
            .ConfigureKestrel(k => k
                .ConfigureHttpsDefaults(h => k
                    .ApplicationServices
                    .GetRequiredService<TlsConfigure>()
                    .Configure(h)));

        return builder;
    }
}
