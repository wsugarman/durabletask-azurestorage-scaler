// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

internal delegate void CertificateFileSystemEventHandler(CertificateFile sender, CertificateFileChangedEventArgs e);

internal class CertificateFile : IDisposable
{
    public string Path { get; }

    public virtual string? KeyPath => null;

    public event CertificateFileSystemEventHandler? Changed;

    private readonly object _lock = new();
    private readonly FileSystemWatcher _watcher;
    private DateTime _lastProcessedWriteTimeUtc;

    public CertificateFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        Path = filePath;
        _watcher = new FileSystemWatcher(System.IO.Path.GetDirectoryName(filePath)!, System.IO.Path.GetFileName(filePath)) { EnableRaisingEvents = true };
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

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Any exceptin is capture and forwarded to subscribers.")]
    protected virtual void OnChanged(FileSystemEventArgs e)
    {
        DateTime lastWriteTimeUtc = DateTime.MinValue;

        lock (_lock)
        {
            try
            {
                // Note: Reading the file from within the OnChanged event handler may conflict
                // with file operations performed by a separate process on the same file.
                lastWriteTimeUtc = File.GetLastWriteTimeUtc(Path);
                if (lastWriteTimeUtc > _lastProcessedWriteTimeUtc)
                {
                    _lastProcessedWriteTimeUtc = lastWriteTimeUtc;
                    Changed?.Invoke(this, new CertificateFileChangedEventArgs { Certificate = Load() });
                }
            }
            catch (Exception ex)
            {
                Changed?.Invoke(this, new CertificateFileChangedEventArgs { Exception = ExceptionDispatchInfo.Capture(ex) });
            }
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
