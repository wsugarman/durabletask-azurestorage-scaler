// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

internal sealed class CertificateFileMonitor : IDisposable
{
    private int _disposed;
    private volatile X509Certificate2? _certificate;
    private volatile ConfigurationReloadToken _changeToken = new();
    private readonly ILogger _logger;
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);

    public X509Certificate2 Current => TryGetCertificate(out X509Certificate2? certificate) ? certificate : GetOrUpdateCertificate();

    public CertificateFile File { get; }

    public CertificateFileMonitor(CertificateFile file, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(logger);

        File = file;
        _logger = logger;

        // Ensure the event handler is configured before loading the certificate
        File.Changed += OnChanged;
        File.EnableRaisingEvents = true;

        _certificate = File.Load();
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
        {
            File.Changed -= OnChanged;

            // Do not dispose any resources until entering write lock
            _lock.EnterWriteLock();

            try
            {
                _certificate?.Dispose();
                _certificate = null;

                File.Dispose();
            }
            finally
            {
                _lock.ExitWriteLock();
                _lock.Dispose();
            }
        }
    }

    public IChangeToken GetReloadToken()
        => _changeToken;

    private bool TryGetCertificate([NotNullWhen(true)] out X509Certificate2? certificate)
    {
        _lock.EnterReadLock();

        try
        {
            ObjectDisposedException.ThrowIf(Thread.VolatileRead(ref _disposed) == 1, this);
            if (_certificate is not null)
            {
                certificate = _certificate;
                return true;
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }

        certificate = default;
        return false;
    }

    [ExcludeFromCodeCoverage(Justification = "It is difficult to deterministically exercise the update code path.")]
    private X509Certificate2 GetOrUpdateCertificate()
    {
        _lock.EnterUpgradeableReadLock();

        try
        {
            ObjectDisposedException.ThrowIf(Thread.VolatileRead(ref _disposed) == 1, this);
            if (_certificate is null)
                UpdateCertificate();

            return _certificate!;
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }
    }

    private void UpdateCertificate(bool throwOnError = true)
    {
        _lock.EnterWriteLock();

        try
        {
            ObjectDisposedException.ThrowIf(Thread.VolatileRead(ref _disposed) == 1, this);

            X509Certificate2 certificate = File.Load();
            _logger.LoadedCertificate(certificate.Thumbprint, File.Path);

            _certificate?.Dispose();
            _certificate = certificate;
        }
        catch (Exception ex) when (ex is not ObjectDisposedException)
        {
            _logger.FailedLoadingCertificate(ex, File.Path);
            _certificate = null;

            if (throwOnError)
                throw;
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        OnReload();
    }

    private void OnChanged(object? sender, FileSystemEventArgs args)
        => UpdateCertificate(throwOnError: false);

    private void OnReload()
    {
        // Subscribers may only be alerted after leaving the lock so that they may read the new value
        ConfigurationReloadToken previousToken = Interlocked.Exchange(ref _changeToken, new ConfigurationReloadToken());
        previousToken.OnReload();
    }
}
