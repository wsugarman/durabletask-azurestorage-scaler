// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

[ExcludeFromCodeCoverage]
internal sealed class TlsConfigure :
    IConfigureNamedOptions<CertificateAuthenticationOptions>,
    IOptionsChangeTokenSource<CertificateAuthenticationOptions>,
    IDisposable
{
    private readonly ILogger _logger;
    private readonly CertificateFileMonitor? _ca;
    private readonly CertificateFileMonitor? _server;

    public TlsConfigure(ILoggerFactory factory, IOptions<TlsClientOptions> clientOptions, IOptions<TlsServerOptions> serverOptions)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(serverOptions?.Value, nameof(serverOptions));
        ArgumentNullException.ThrowIfNull(clientOptions?.Value, nameof(clientOptions));

        _logger = factory.CreateLogger(LogCategories.Security);
        if (!string.IsNullOrWhiteSpace(clientOptions.Value.CaCertificatePath))
            _ca = new CertificateFile(clientOptions.Value.CaCertificatePath).Monitor(_logger);

        if (!string.IsNullOrWhiteSpace(serverOptions.Value.CertificatePath))
            _server = CertificateFile.CreateFromPemFile(serverOptions.Value.CertificatePath, serverOptions.Value.KeyPath).Monitor(_logger);
    }

    string? IOptionsChangeTokenSource<CertificateAuthenticationOptions>.Name => CertificateAuthenticationDefaults.AuthenticationScheme;

    public void Configure(HttpsConnectionAdapterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (_ca is not null)
        {
            options.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
            _logger.RequiredClientCertificate();
        }

        if (_server is not null)
        {
            options.ServerCertificateSelector = (c, s) => _server.Current;
            _logger.ConfiguredServerCertificate(_server.File.Path, _server.File.KeyPath);
        }
    }

    public void Configure(CertificateAuthenticationOptions options)
         => Configure(Options.DefaultName, options);

    public void Configure(string? name, CertificateAuthenticationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.Equals(name, CertificateAuthenticationDefaults.AuthenticationScheme, StringComparison.Ordinal) && _ca is not null)
        {
            options.ChainTrustValidationMode = X509ChainTrustMode.CustomRootTrust;
            options.CustomTrustStore.Clear();
            _ = options.CustomTrustStore.Add(_ca.Current);

            _logger.ConfiguredClientCertificateValidation(_ca.File.Path);
        }
    }

    public void Dispose()
    {
        _ca?.Dispose();
        _server?.Dispose();
        GC.SuppressFinalize(this);
    }

    public IChangeToken GetChangeToken()
        => _ca?.GetReloadToken() ?? NullChangeToken.Singleton;
}
