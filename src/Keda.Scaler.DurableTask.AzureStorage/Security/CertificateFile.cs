// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

internal class CertificateFile : IDisposable
{
    private readonly FileSystemWatcher _watcher;

    public string Path { get; }

    public virtual string? KeyPath => null;

    public bool EnableRaisingEvents
    {
        get => _watcher.EnableRaisingEvents;
        set => _watcher.EnableRaisingEvents = value;
    }

    public event FileSystemEventHandler? Changed
    {
        add => _watcher.Changed += value;
        remove => _watcher.Changed -= value;
    }

    public CertificateFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        Path = filePath;
        _watcher = new FileSystemWatcher(System.IO.Path.GetDirectoryName(filePath)!, System.IO.Path.GetFileName(filePath));
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public CertificateFileMonitor Monitor(ILogger logger)
        => new(this, logger);

    public virtual X509Certificate2 Load()
        => X509CertificateLoader.LoadCertificateFromFile(Path);

    public static CertificateFile CreateFromPemFile(string certPemFilePath, string? keyPemFilePath = default)
        => new CertificatePemFile(certPemFilePath, keyPemFilePath);

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
            _watcher.Dispose();
    }

    private sealed class CertificatePemFile(string certPemFilePath, string? keyPemFilePath = default)
        : CertificateFile(certPemFilePath)
    {
        public override string? KeyPath { get; } = keyPemFilePath;

        public override X509Certificate2 Load()
            => X509Certificate2.CreateFromPemFile(Path, KeyPath);
    }
}
