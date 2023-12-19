// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
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
            ObjectDisposedException.ThrowIf(Thread.VolatileRead(ref _disposed) == 1, this);

            _lock.EnterReadLock();

            try
            {
                ObjectDisposedException.ThrowIf(Thread.VolatileRead(ref _disposed) == 1, this);
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

    private int _disposed;
    private volatile X509Certificate2? _certificate;
    private volatile ExceptionDispatchInfo? _loadError;
    private volatile ConfigurationReloadToken _changeToken = new(); // Reuse ConfigurationReloadToken for simplicity
    private readonly ILogger _logger;
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);

    public CertificateFileMonitor(CertificateFile file, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(logger);

        File = file;
        _logger = logger;

        // Ensure the event handler is configured before loading the certificate
        File.Changed += ReloadCertificate;
        File.EnableRaisingEvents = true;
        LoadCertificate();
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
        {
            // Do not dispose any resources until entering write lock
            _lock.EnterWriteLock();

            try
            {
                _certificate?.Dispose();
                _certificate = null;

                File.Changed -= ReloadCertificate;
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

    private void LoadCertificate()
        => ReloadCertificate(File, new CertificateFileChangedEventArgs { Certificate = File.Load() });

    private void ReloadCertificate(CertificateFile sender, CertificateFileChangedEventArgs args)
    {
        _lock.EnterWriteLock();

        ConfigurationReloadToken previousToken;
        try
        {
            ObjectDisposedException.ThrowIf(Thread.VolatileRead(ref _disposed) == 1, this);
            if (args.Certificate is not null)
            {
                _logger.LoadedCertificate(args.Certificate.Thumbprint, sender.Path);

                _certificate?.Dispose();
                _certificate = args.Certificate;
                _loadError = null;
            }
            else
            {
                _logger.FailedLoadingCertificate(args.Exception!.SourceException, sender.Path);

                _certificate = null;
                _loadError = args.Exception;
            }

            // Subscribers may only be alerted after leaving the lock so that they may read the new value
            previousToken = Interlocked.Exchange(ref _changeToken, new ConfigurationReloadToken());
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        previousToken.OnReload();
    }
}
