// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using Keda.Scaler.DurableTask.AzureStorage.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

internal static class WebApplicationBuilderExtensions
{
    [ExcludeFromCodeCoverage]
    public static WebApplicationBuilder ConfigureKestrelTls(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.WebHost
            .ConfigureKestrel(k => k
                .ConfigureHttpsDefaults(h => k
                    .ApplicationServices
                    .GetRequiredService<TlsConnectionAdapterOptionsConfigure>()
                    .Configure(h)));

        return builder;
    }
}
