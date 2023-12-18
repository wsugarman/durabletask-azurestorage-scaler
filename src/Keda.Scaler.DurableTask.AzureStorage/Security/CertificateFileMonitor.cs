// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

internal sealed class CertificateFileMonitor : IDisposable
{
    public X509Certificate2 Current
    {
        get
        {
            _lock.EnterReadLock();

            try
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                _loadError?.Throw();

                return _certificate!;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    public CertificateFile File { get; }

    private bool _disposed;
    private X509Certificate2? _certificate;
    private ExceptionDispatchInfo? _loadError;
    private readonly ILogger _logger;
    private volatile ConfigurationReloadToken _changeToken = new(); // Reuse ConfigurationReloadToken for simplicity

    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Lock used for synchronizing disposal. Disposed in finalizer instead.")]
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);

    public CertificateFileMonitor(CertificateFile file, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(logger);

        File = file;
        _logger = logger;

        // Ensure the event handler is configured before loading the certificate
        File.Changed += ReloadCertificate;
        LoadCertificate();
    }

    ~CertificateFileMonitor()
        => _lock.Dispose();

    [SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "Finalizer disposes of ReaderWriterLockSlim.")]
    public void Dispose()
    {
        _lock.EnterWriteLock();

        try
        {
            // Check if the certificate has already been disposed
            if (!_disposed)
            {
                _certificate?.Dispose();
                _certificate = null;

                File.Changed -= ReloadCertificate;
                File.Dispose();

                _disposed = true;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public IChangeToken GetReloadToken()
        => _changeToken;

    private void LoadCertificate()
        => ReloadCertificate(File, new CertificateFileChangedEventArgs { Certificate = File.Load() });

    private void ReloadCertificate(CertificateFile sender, CertificateFileChangedEventArgs args)
    {
        bool notifySubscribers;

        _lock.EnterWriteLock();

        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (args.Certificate is not null)
            {
                _logger.LoadedCertificate(args.Certificate.Thumbprint, sender.Path);

                // Determine whether the thumbprint has notifySubscribers
                // If the thumbprint hasn't notifySubscribers, no need to alert downstream subscribers
                notifySubscribers = !string.Equals(_certificate?.Thumbprint, args.Certificate.Thumbprint, StringComparison.Ordinal);
                if (!notifySubscribers)
                    _logger.SkippedReloadEventForDuplicateThumbprint(args.Certificate.Thumbprint);

                _certificate?.Dispose();
                _certificate = args.Certificate;
                _loadError = null;
            }
            else
            {
                _logger.FailedLoadingCertificate(args.Exception!.SourceException, sender.Path);

                // Determine whether the certificate continues to fail to load
                // If the certificate is still in error, no need to alert downstream subscribers
                notifySubscribers = _loadError is null;
                if (!notifySubscribers)
                    _logger.SkippedReloadEventForContinuedError();

                _certificate = null;
                _loadError = args.Exception;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        // Subscribers may only be alerted after leaving the lock so that they may read the new value
        if (notifySubscribers)
        {
            ConfigurationReloadToken previousToken = Interlocked.Exchange(ref _changeToken, new ConfigurationReloadToken());
            previousToken.OnReload();
        }
    }
}
