// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

internal sealed class TlsConfigure : IConfigureNamedOptions<CertificateAuthenticationOptions>, IOptionsChangeTokenSource<CertificateAuthenticationOptions>
{
    private readonly bool _validateClientCertificate;
    private readonly CertificateFileMonitor? _server;
    private readonly CertificateFileMonitor? _clientCa;
    private readonly ILogger _logger;

    public TlsConfigure(
        IOptions<TlsClientOptions> clientOptions,
        ILoggerFactory loggerFactory,
        [FromKeyedServices("server")] CertificateFileMonitor? server = null,
        [FromKeyedServices("clientca")] CertificateFileMonitor? clientCa = null)
    {
        ArgumentNullException.ThrowIfNull(clientOptions?.Value, nameof(clientOptions));
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _logger = loggerFactory.CreateLogger(LogCategories.Security);
        _server = server;

        // Values may be inconsistent based on user settings, so only assign values if appropriate
        // E.g. We will not validate client certificates if the server is not returning its own certificate
        if (_server is not null)
        {
            _validateClientCertificate = clientOptions.Value.ValidateCertificate;
            if (_validateClientCertificate)
                _clientCa = clientCa;
        }
    }

    string? IOptionsChangeTokenSource<CertificateAuthenticationOptions>.Name => CertificateAuthenticationDefaults.AuthenticationScheme;

    public void Configure(HttpsConnectionAdapterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (_server is not null)
        {
            options.AllowAnyClientCertificate(); // Use certitificate middleware for validation
            options.ServerCertificateSelector = (c, s) => _server.Current;
            _logger.ConfiguredServerCertificate(_server.File.Path, _server.File.KeyPath);

            if (_validateClientCertificate)
            {
                options.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                _logger.RequiredClientCertificate();
            }
        }
    }

    public void Configure(CertificateAuthenticationOptions options)
         => Configure(Options.DefaultName, options);

    public void Configure(string? name, CertificateAuthenticationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.Equals(name, CertificateAuthenticationDefaults.AuthenticationScheme, StringComparison.Ordinal) && _clientCa is not null)
        {
            options.ChainTrustValidationMode = X509ChainTrustMode.CustomRootTrust;
            options.CustomTrustStore.Clear();
            _ = options.CustomTrustStore.Add(_clientCa.Current);

            _logger.ConfiguredClientCertificateValidation(_clientCa.File.Path);
        }
    }

    public IChangeToken GetChangeToken()
        => _clientCa?.GetReloadToken() ?? NullChangeToken.Singleton;
}
