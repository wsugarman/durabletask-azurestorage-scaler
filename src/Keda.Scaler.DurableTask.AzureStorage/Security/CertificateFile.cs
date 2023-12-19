// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

internal delegate void CertificateFileSystemEventHandler(CertificateFile sender, CertificateFileChangedEventArgs e);

internal class CertificateFile : IDisposable
{
    public string Path { get; }

    public virtual string? KeyPath => null;

    public bool EnableRaisingEvents
    {
        get => _watcher.EnableRaisingEvents;
        set => _watcher.EnableRaisingEvents = value;
    }

    public event CertificateFileSystemEventHandler? Changed;

    private readonly object _lock = new();
    private readonly FileSystemWatcher _watcher;
    private DateTime _lastProcessedWriteTimeUtc;

    public CertificateFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        Path = filePath;
        _watcher = new FileSystemWatcher(System.IO.Path.GetDirectoryName(filePath)!, System.IO.Path.GetFileName(filePath));
        _watcher.Changed += (o, e) => OnChanged(e);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public CertificateFileMonitor Monitor(ILogger logger)
        => new(this, logger);

    public virtual X509Certificate2 Load()
        => new(Path);

    public static CertificateFile CreateFromPemFile(string certPemFilePath, string? keyPemFilePath = default)
        => new CertificatePemFile(certPemFilePath, keyPemFilePath);

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
            _watcher.Dispose();
    }

    [ExcludeFromCodeCoverage]
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Ignore IO errors and allow subscriber to discover them.")]
    protected virtual void OnChanged(FileSystemEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        DateTime lastWriteTimeUtc = DateTime.MinValue;

        lock (_lock)
        {
            try
            {
                lastWriteTimeUtc = File.GetLastWriteTimeUtc(e.FullPath);
                if (lastWriteTimeUtc <= _lastProcessedWriteTimeUtc)
                    return;

                _lastProcessedWriteTimeUtc = lastWriteTimeUtc;
            }
            catch (Exception)
            { }

            Changed?.Invoke(this, new CertificateFileChangedEventArgs { ChangeType = e.ChangeType });
        }
    }

    private sealed class CertificatePemFile(string certPemFilePath, string? keyPemFilePath = default)
        : CertificateFile(certPemFilePath)
    {
        public override string? KeyPath { get; } = keyPemFilePath;

        public override X509Certificate2 Load()
            => X509Certificate2.CreateFromPemFile(Path, KeyPath);
    }
}
