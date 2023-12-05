// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.Web;

internal static class WebApplicationExtensions
{
    public static bool RequiresClientCertificate(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return string.IsNullOrWhiteSpace(app.Services.GetRequiredService<IOptions<TlsClientOptions>>().Value.CaCertificatePath);
    }
}
