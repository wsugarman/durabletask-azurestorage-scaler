// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

internal class CertificateFile : IDisposable
{
    public string Path { get; }

    private readonly PhysicalFileProvider _fileProvider;

    public CertificateFile(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        Path = fileName;
        _fileProvider = new PhysicalFileProvider(System.IO.Path.GetDirectoryName(fileName)!);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public CertificateFileMonitor Monitor(ILogger? logger = default)
        => CertificateFileMonitor.Create(this, logger);

    public virtual X509Certificate2 Load()
        => new(Path);

    public IChangeToken Watch()
        => _fileProvider.Watch(System.IO.Path.GetFileName(Path));

    public static CertificateFile CreateFromPemFile(string certPemFilePath, string? keyPemFilePath = default)
        => new CertificatePemFile(certPemFilePath, keyPemFilePath);

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
            _fileProvider.Dispose();
    }

    private sealed class CertificatePemFile(string certPemFilePath, string? keyPemFilePath = default)
        : CertificateFile(certPemFilePath)
    {
        public string? KeyPath { get; } = keyPemFilePath;

        public override X509Certificate2 Load()
            => X509Certificate2.CreateFromPemFile(Path, KeyPath);
    }
}
