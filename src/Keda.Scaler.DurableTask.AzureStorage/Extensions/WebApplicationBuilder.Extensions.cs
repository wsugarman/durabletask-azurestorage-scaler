// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Keda.Scaler.DurableTask.AzureStorage.Common;
using Keda.Scaler.DurableTask.AzureStorage.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.DependencyInjection;

internal static class WebApplicationBuilderExtensions
{
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposable object is returned to caller for disposal.")]
    public static IDisposable ConfigureKestrelTls(this WebApplicationBuilder builder)
    {
        if (builder is null)
            throw new ArgumentNullException(nameof(builder));

        // Check security settings
        TlsOptions tlsOptions = new TlsOptions();
        builder.Configuration.GetSection(TlsOptions.DefaultSectionName).Bind(tlsOptions);

        if (tlsOptions.CertificatePath is null)
            return NullDisposable.Instance;

        string certificateFileName = Path.GetFileName(tlsOptions.CertificatePath);
        PhysicalFileProvider watcher = new PhysicalFileProvider(Path.GetDirectoryName(tlsOptions.CertificatePath)!);
        Monitored<X509Certificate2> cert = new Monitored<X509Certificate2>(
            () => tlsOptions.ReadCertificate()!,
            () => watcher.Watch(certificateFileName));

        builder.WebHost
            .ConfigureKestrel(o => o
                .ConfigureHttpsDefaults(x =>
                {
                    x.ClientCertificateMode = tlsOptions.MutualTls ? ClientCertificateMode.RequireCertificate : ClientCertificateMode.AllowCertificate;
                    x.ServerCertificateSelector = (context, dnsName) => cert.Current;
                }));

        return watcher;
    }
}
