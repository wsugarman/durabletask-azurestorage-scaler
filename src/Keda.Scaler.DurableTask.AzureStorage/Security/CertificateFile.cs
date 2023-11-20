// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

internal class CertificateFile : IDisposable
{
    public X509Certificate2 Current
    {
        get
        {
            ReadOperation? readOperation = _readOperation;
            ObjectDisposedException.ThrowIf(readOperation is null, this);

            if (readOperation.Exception is not null)
                readOperation.Exception!.Throw();

            return readOperation.Certificate!;
        }
    }

    private readonly string _path;
    private readonly PhysicalFileProvider _fileProvider;
    private readonly object _lock = new();

    private ReadOperation? _readOperation;
    private IDisposable? _receipt;
    private ConfigurationReloadToken _changeToken = new(); // Reuse ConfigurationReloadToken for simplicity

    private CertificateFile(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        _path = fileName;
        _fileProvider = new PhysicalFileProvider(Path.GetDirectoryName(fileName)!);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            // _receipt = null is a proxy for disposal
            if (_receipt is not null)
            {
                _receipt.Dispose();
                _fileProvider.Dispose();
                _readOperation?.Dispose();

                _receipt = null;
                _readOperation = null;

                GC.SuppressFinalize(this);
            }
        }
    }

    public IChangeToken Watch()
        => _changeToken;

    protected virtual X509Certificate2 ReadCertificate(string fileName)
        => new(fileName);

    private CertificateFile BeginMonitoring()
    {
        // Configure the monitor before load the file so we do not miss any changes
        _receipt = ChangeToken.OnChange(WatchSourceFile, Reload);
        Load();

        return this;
    }

    private void Load()
    {
        // When loading for (usually) the first time, no need to capture any errors or alert the 0 listeners
        lock (_lock)
        {
            ReadOperation read = new() { Certificate = ReadCertificate(_path) };
            ReadOperation? previousRead = Interlocked.Exchange(ref _readOperation, read);
            previousRead?.Dispose();
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Any exception thrown here will be missed by users.")]
    private void Reload()
    {
        // Lock ensures that Reload cannot occur in the middle of disposal (or the initialization)
        // We do not want to risk the certificate being disposed in the middle of a consumer's OnReload callback.
        lock (_lock)
        {
            if (_receipt is not null)
            {
                // Attempt to read the certificate. However, if there are any errors, preserve them for the user later.
                ReadOperation? read;

                try
                {
                    read = new ReadOperation { Certificate = ReadCertificate(_path) };
                }
                catch (Exception e)
                {
                    read = new ReadOperation { Exception = ExceptionDispatchInfo.Capture(e) };
                }

                ReadOperation? previousRead = Interlocked.Exchange(ref _readOperation, read);
                previousRead?.Dispose();

                // Alert any listeners of the certificate change (or error)
                ConfigurationReloadToken previousToken = Interlocked.Exchange(ref _changeToken, new ConfigurationReloadToken());
                previousToken.OnReload();
            }
        }
    }

    private IChangeToken WatchSourceFile()
        => _fileProvider.Watch(Path.GetFileName(_path));

    public static CertificateFile From(string fileName)
        => new CertificateFile(fileName).BeginMonitoring();

    public static CertificateFile FromPemFile(string certPemFilePath, string? keyPemFilePath = default)
        => new CertificatePemFile(certPemFilePath, keyPemFilePath).BeginMonitoring();

    private sealed class CertificatePemFile(string certPemFilePath, string? keyPemFilePath = null)
        : CertificateFile(certPemFilePath)
    {
        private readonly string? _keyPath = keyPemFilePath;

        protected override X509Certificate2 ReadCertificate(string fileName)
            => X509Certificate2.CreateFromPemFile(fileName, _keyPath);
    }

    private sealed class ReadOperation : IDisposable
    {
        public X509Certificate2? Certificate { get; init; }

        public ExceptionDispatchInfo? Exception { get; init; }

        public void Dispose()
        {
            Certificate?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
