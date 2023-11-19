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

internal sealed class CertificateFile : IDisposable
{
    public X509Certificate2 Current => _cert;

    private readonly string _path;
    private readonly string? _keyPath;
    private readonly PhysicalFileProvider _fileProvider;
    private readonly IDisposable _receipt;
    private readonly object _lock = new();

    private volatile X509Certificate2 _cert;
    private volatile bool _disposed;
    private ConfigurationReloadToken _changeToken = new(); // Reuse ConfigurationReloadToken for simplicity

    public CertificateFile(string path, string? keyPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        _path = path;
        _keyPath = keyPath;
        _fileProvider = new PhysicalFileProvider(Path.GetDirectoryName(path)!);
        _receipt = ChangeToken.OnChange(WatchInput, Reload);
        _cert = LoadCertificate();
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _receipt.Dispose();
            _cert.Dispose();
            _fileProvider.Dispose();
            _disposed = true;

            GC.SuppressFinalize(this);
        }
    }

    public IChangeToken Watch()
        => _changeToken;

    private void Reload()
    {
        lock (_lock)
        {
            if (!_disposed)
            {
                _cert = LoadCertificate();

                // After reloading, alert any listeners
                ConfigurationReloadToken previousToken = Interlocked.Exchange(ref _changeToken, new ConfigurationReloadToken());
                previousToken.OnReload();
            }
        }
    }

    private X509Certificate2 LoadCertificate()
        => X509Certificate2.CreateFromPemFile(_path, _keyPath);

    private IChangeToken WatchInput()
        => _fileProvider.Watch(Path.GetFileName(_path));
}
