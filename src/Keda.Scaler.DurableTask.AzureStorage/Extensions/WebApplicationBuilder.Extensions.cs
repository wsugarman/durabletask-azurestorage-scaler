// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Keda.Scaler.DurableTask.AzureStorage.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.DependencyInjection;

internal static class WebApplicationBuilderExtensions
{
    [ExcludeFromCodeCoverage]
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Objects persist for the lifetime of the application and are captured by closures.")]
    public static WebApplicationBuilder ConfigureKestrelTls(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Check security settings
        TlsOptions? tlsOptions = builder
            .Configuration
            .GetSection(TlsOptions.DefaultKey)
            .Get<TlsOptions>();

        if (!string.IsNullOrWhiteSpace(tlsOptions?.CertificatePath))
        {
            string certificateFileName = Path.GetFileName(tlsOptions.CertificatePath);
            PhysicalFileProvider watcher = new(Path.GetDirectoryName(tlsOptions.CertificatePath)!);
            Monitored<X509Certificate2> cert = new(
                () => X509Certificate2.CreateFromPemFile(tlsOptions.CertificatePath, tlsOptions.KeyPath),
                () => watcher.Watch(certificateFileName));

            // TODO: Enable mTLS once supported by KEDA
            _ = builder.WebHost.ConfigureKestrel(o => o.ConfigureHttpsDefaults(x => x.ServerCertificateSelector = (context, dnsName) => cert.Current));
        }

        return builder;
    }
}
