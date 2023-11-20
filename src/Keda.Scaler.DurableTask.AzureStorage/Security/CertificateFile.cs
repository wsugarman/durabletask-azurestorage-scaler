// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

internal class CertificateFile : IDisposable
{
    public X509Certificate2 Current => _cert ?? throw new ObjectDisposedException(nameof(CertificateFile));

    private readonly string _path;
    private readonly PhysicalFileProvider _fileProvider;
    private readonly IDisposable _receipt;
    private readonly object _lock = new();

    // This method is used to load the certificates from the ctor,
    // and as such needs to capture state from potential derived classes
    private readonly Func<string, X509Certificate2> _loadCertificate;

    private X509Certificate2? _cert;
    private ConfigurationReloadToken _changeToken = new(); // Reuse ConfigurationReloadToken for simplicity

    public CertificateFile(string path)
        : this(path, s => new(s))
    { }

    protected CertificateFile(string fileName, Func<string, X509Certificate2> loadCertificate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(loadCertificate);

        _path = fileName;
        _fileProvider = new PhysicalFileProvider(Path.GetDirectoryName(fileName)!);
        _loadCertificate = loadCertificate;
        _receipt = ChangeToken.OnChange(WatchInput, Reload);

        Reload(always: true);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_cert is not null)
            {
                _receipt.Dispose();
                _fileProvider.Dispose();
                _cert.Dispose();
                _cert = null;

                GC.SuppressFinalize(this);
            }
        }
    }

    public IChangeToken Watch()
        => _changeToken;

    private void Reload()
        => Reload(always: false);

    private void Reload(bool always)
    {
        // Lock ensures that Reload cannot occur in the middle of disposal (or the initialization)
        // We do not want to risk the certificate being disposed in the middle of a consumer's OnReload callback.
        lock (_lock)
        {
            if (_cert is not null || always)
            {
                X509Certificate2? previousCert = Interlocked.Exchange(ref _cert, _loadCertificate(_path));
                previousCert?.Dispose();

                // After reloading, alert any listeners
                ConfigurationReloadToken previousToken = Interlocked.Exchange(ref _changeToken, new ConfigurationReloadToken());
                previousToken.OnReload();
            }
        }
    }

    private IChangeToken WatchInput()
        => _fileProvider.Watch(Path.GetFileName(_path));

    public static CertificateFile CreateFromPemFile(string certPemFilePath, string? keyPemFilePath = default)
        => new CertificatePemFile(certPemFilePath, keyPemFilePath);

    private sealed class CertificatePemFile(string certPemFilePath, string? keyPemFilePath = null)
        : CertificateFile(certPemFilePath, p => X509Certificate2.CreateFromPemFile(p, keyPemFilePath))
    { }
}
