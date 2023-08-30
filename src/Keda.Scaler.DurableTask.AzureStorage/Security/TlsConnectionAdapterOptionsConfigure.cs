// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

[ExcludeFromCodeCoverage]
internal sealed class TlsConnectionAdapterOptionsConfigure : IDisposable
{
    private readonly MonitoredCertificate? _ca;
    private readonly MonitoredCertificate? _server;
    private readonly ILogger _logger;

    private static readonly Oid ClientCertificateOid = new Oid("1.3.6.1.5.5.7.3.2");

    public TlsConnectionAdapterOptionsConfigure(ILoggerFactory loggerFactory, IOptions<TlsOptions> options)
    {
        if (loggerFactory is null)
            throw new ArgumentNullException(nameof(loggerFactory));

        if (options?.Value is null)
            throw new ArgumentNullException(nameof(options));

        if (!string.IsNullOrWhiteSpace(options.Value.ClientCaCertificatePath))
            _ca = new MonitoredCertificate(options.Value.ClientCaCertificatePath);

        if (!string.IsNullOrWhiteSpace(options.Value.ServerCertificatePath))
            _server = new MonitoredCertificate(options.Value.ServerCertificatePath, options.Value.ServerKeyPath);

        _logger = loggerFactory.CreateLogger(Diagnostics.SecurityLoggerCategory);
    }

    public void Configure(HttpsConnectionAdapterOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (_ca is not null)
        {
            options.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
            options.ClientCertificateValidation = ValidateClientCertificate;
        }

        if (_server is not null)
            options.ServerCertificateSelector = (c, s) => _server.Current;
    }

    public void Dispose()
    {
        _ca?.Dispose();
        _server?.Dispose();
        GC.SuppressFinalize(this);
    }

    private bool ValidateClientCertificate(X509Certificate2 clientCertificate, X509Chain? existingChain, SslPolicyErrors sslPolicyErrors)
    {
        // Check for any existing errors from validation
        if ((existingChain?.ChainStatus.Any(x => x.Status is not X509ChainStatusFlags.NoError)).GetValueOrDefault())
        {
            LogChainErrors(clientCertificate, existingChain!);
            return false;
        }

        if (sslPolicyErrors is not SslPolicyErrors.None)
            return false;

        // Build a new chain with a custom policy that asserts the certificate authority
        using var chain = new X509Chain { ChainPolicy = BuildChainPolicy() };

        if (!chain.Build(clientCertificate))
        {
            LogChainErrors(clientCertificate, chain);
            return false;
        }

        return true;
    }

    private X509ChainPolicy BuildChainPolicy()
    {
        var chainPolicy = new X509ChainPolicy
        {
            ApplicationPolicy = { ClientCertificateOid },
            CustomTrustStore = { _ca!.Current },
            RevocationFlag = X509RevocationFlag.ExcludeRoot,
            RevocationMode = X509RevocationMode.Online,
            TrustMode = X509ChainTrustMode.CustomRootTrust,
        };

        chainPolicy.VerificationFlags |= X509VerificationFlags.IgnoreNotTimeValid;

        return chainPolicy;
    }

    private void LogChainErrors(X509Certificate2 certificate, X509Chain chain)
    {
        var chainErrors = new List<string>(chain.ChainStatus.Length);
        foreach (X509ChainStatus validationFailure in chain.ChainStatus)
            chainErrors.Add($"{validationFailure.Status} {validationFailure.StatusInformation}");

        _logger.LogWarning("Certificate validation failed, subject was {Subject}. {ChainErrors}", certificate.Subject, chainErrors);
    }
}
