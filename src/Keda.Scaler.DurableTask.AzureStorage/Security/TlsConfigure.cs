// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

[ExcludeFromCodeCoverage]
internal sealed class TlsConfigure :
    IConfigureOptions<CertificateAuthenticationOptions>,
    IOptionsChangeTokenSource<CertificateAuthenticationOptions>,
    IDisposable
{
    private readonly CertificateFile? _ca;
    private readonly CertificateFile? _server;

    public TlsConfigure(IOptions<TlsClientOptions> clientOptions, IOptions<TlsServerOptions> serverOptions)
    {
        ArgumentNullException.ThrowIfNull(serverOptions?.Value, nameof(serverOptions));
        ArgumentNullException.ThrowIfNull(clientOptions?.Value, nameof(clientOptions));

        if (!string.IsNullOrWhiteSpace(clientOptions.Value.CaCertificatePath))
            _ca = new CertificateFile(clientOptions.Value.CaCertificatePath);

        if (!string.IsNullOrWhiteSpace(serverOptions.Value.CertificatePath))
            _server = new CertificateFile(serverOptions.Value.CertificatePath, serverOptions.Value.KeyPath);
    }

    public string? Name => Options.DefaultName;

    public void Configure(HttpsConnectionAdapterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.ClientCertificateMode = _ca is null ? ClientCertificateMode.NoCertificate : ClientCertificateMode.RequireCertificate;
        options.ServerCertificateSelector = _server is null ? null : (c, s) => _server.Current;
    }

    public void Configure(CertificateAuthenticationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (_ca is not null)
        {
            options.CustomTrustStore.Clear();
            _ = options.CustomTrustStore.Add(_ca.Current);
        }
    }

    public void Dispose()
    {
        _ca?.Dispose();
        _server?.Dispose();
        GC.SuppressFinalize(this);
    }

    public IChangeToken GetChangeToken()
        => _ca?.Watch() ?? NullChangeToken.Singleton;
}
