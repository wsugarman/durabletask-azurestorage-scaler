// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

internal sealed class CertificateFileMonitor : IDisposable
{
    public X509Certificate2 Current => UpdateCertificate(force: false);

    public CertificateFile File { get; }

    private X509Certificate2? _certificate;
    private IDisposable? _subscription;
    private ConfigurationReloadToken _changeToken = new(); // Reuse ConfigurationReloadToken for simplicity

    private CertificateFileMonitor(CertificateFile file)
        => File = file ?? throw new ArgumentNullException(nameof(file));

    public void Dispose()
    {
        // Attempt to set the disposed flag
        X509Certificate2? expected;
        for (expected = _certificate; !ReferenceEquals(expected, States.Disposed); expected = _certificate)
        {
            if (ReferenceEquals(expected, Interlocked.CompareExchange(ref _certificate, States.Disposed, expected)))
                break;
        }

        // If this thread was successful in starting disposal, then dispose!
        if (!ReferenceEquals(expected, States.Disposed))
        {
            expected?.Dispose();
            _subscription?.Dispose();
            File.Dispose();

            _subscription = null;

            GC.SuppressFinalize(this);
        }
    }

    public IChangeToken GetReloadToken()
        => _changeToken;

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Only throw exceptions on request threads.")]
    private IDisposable Subscribe(ILogger logger)
    {
        // Configure the monitor before load the file so we do not miss any changes
        // Note: Users cannot Dispose this instance before this method exits,
        // so do not worry about concurrency related to setting _subscription
        _subscription = ChangeToken.OnChange(File.Watch, Reload);
        _ = UpdateCertificate(force: false);

        return _subscription;

        void Reload()
        {
            try
            {
                X509Certificate2 latest = UpdateCertificate(force: true);
                logger.LoadedCertificate(File.Path, latest.Thumbprint);
            }
            catch (Exception ex)
            {
                logger.FailedLoadingCertificate(ex, File.Path);
            }
        }
    }

    private X509Certificate2 UpdateCertificate(bool force)
    {
        X509Certificate2? expected;
        for (expected = _certificate; !ReferenceEquals(expected, States.Disposed) && (force || expected is null); expected = _certificate)
        {
            X509Certificate2 latest;
            try
            {
                latest = File.Load();
            }
            catch (Exception)
            {
                // If the file fails to load, invalidate the certificate
                if (ReferenceEquals(expected, Interlocked.CompareExchange(ref _certificate, null, expected)))
                    OnReload(expected);

                throw;
            }

            X509Certificate2? actual = Interlocked.CompareExchange(ref _certificate, latest, expected);

            // Did this thread succeed in updating?
            if (ReferenceEquals(expected, actual))
            {
                OnReload(actual);
                return latest;
            }

            // Otherwise, clean up this unused certificate
            string thumbprint = latest.Thumbprint;
            latest.Dispose();

            // Did another thread load the same certificate?
            if (string.Equals(thumbprint, actual?.Thumbprint, StringComparison.Ordinal))
                return actual!;
        }

        ObjectDisposedException.ThrowIf(ReferenceEquals(expected, States.Disposed), this);
        return expected;
    }

    private void OnReload(X509Certificate2? previous)
    {
        previous?.Dispose();

        // Alert any listeners of the expected change
        ConfigurationReloadToken previousToken = Interlocked.Exchange(ref _changeToken, new ConfigurationReloadToken());
        previousToken.OnReload();
    }

    public static CertificateFileMonitor Create(CertificateFile file, ILogger? logger = null)
    {
        CertificateFileMonitor monitor = new(file);
        _ = monitor.Subscribe(logger ?? NullLogger.Instance);

        return monitor;
    }

    private static class States
    {
        public static readonly X509Certificate2 Disposed = new(ReadOnlySpan<byte>.Empty);
    }
}
