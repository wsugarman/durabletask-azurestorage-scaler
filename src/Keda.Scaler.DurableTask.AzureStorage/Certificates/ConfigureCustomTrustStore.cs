// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Keda.Scaler.DurableTask.AzureStorage.Certificates;

internal sealed class ConfigureCustomTrustStore : IConfigureNamedOptions<CertificateAuthenticationOptions>, IDisposable, IOptionsChangeTokenSource<CertificateAuthenticationOptions>
{
    private readonly CaCertificateFileOptions _options;
    private readonly ReaderWriterLockSlim _certificateLock;
    private readonly PhysicalFileProvider _fileProvider;
    private readonly IDisposable _changeTokenRegistration;
    private X509Certificate2Collection _certificates;
    private ConfigurationReloadToken _reloadToken;

    public string? Name => CertificateAuthenticationDefaults.AuthenticationScheme;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Certificate disposed in collection.")]
    public ConfigureCustomTrustStore(IOptions<ClientCertificateValidationOptions> options, ReaderWriterLockSlim certificateLock)
    {
        ArgumentNullException.ThrowIfNull(options?.Value?.CertificateAuthority, nameof(options));
        ArgumentNullException.ThrowIfNull(certificateLock);

        _options = options.Value.CertificateAuthority;
        _certificateLock = certificateLock;
        _certificates = [_options.Load()];
        _fileProvider = new PhysicalFileProvider(Path.GetDirectoryName(_options.Path)!);
        _reloadToken = new ConfigurationReloadToken();
        _changeTokenRegistration = ChangeToken.OnChange(
            () => _fileProvider.Watch(Path.GetFileName(_options.Path)),
            () =>
            {
                Thread.Sleep(_options.ReloadDelayMs);
                Reload(_options.Load());
            });
    }

    public void Configure(CertificateAuthenticationOptions options)
        => Configure(Options.DefaultName, options);

    public void Configure(string? name, CertificateAuthenticationOptions options)
    {
        options.ChainTrustValidationMode = X509ChainTrustMode.CustomRootTrust;
        options.CustomTrustStore = _certificates;
    }

    public void Dispose()
    {
        _certificates[0].Dispose();
        _changeTokenRegistration.Dispose();
        _fileProvider.Dispose();
        GC.SuppressFinalize(this);
    }

    public IChangeToken GetChangeToken()
        => _reloadToken;

    private void Reload(X509Certificate2 certificate)
    {
        try
        {
            _certificateLock.EnterWriteLock();

            _certificates = [certificate];
            ConfigurationReloadToken previousToken = Interlocked.Exchange(ref _reloadToken, new ConfigurationReloadToken());
            previousToken.OnReload();
        }
        finally
        {
            _certificateLock.ExitWriteLock();
        }
    }
}
