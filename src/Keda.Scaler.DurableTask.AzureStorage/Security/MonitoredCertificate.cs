// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.FileProviders;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

[ExcludeFromCodeCoverage]
internal sealed class MonitoredCertificate : IDisposable
{
    public X509Certificate2 Current => _value.Current;

    private readonly PhysicalFileProvider _fileWatcher;
    private readonly Monitored<X509Certificate2> _value;

    public MonitoredCertificate(string path, string? keyPath = null)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentNullException(nameof(path));

        string certificateFileName = Path.GetFileName(path);
        _fileWatcher = new PhysicalFileProvider(Path.GetDirectoryName(path)!);
        _value = new Monitored<X509Certificate2>(
            () => X509Certificate2.CreateFromPemFile(path, keyPath),
            () => _fileWatcher.Watch(certificateFileName));
    }

    public void Dispose()
    {
        _value?.Dispose();
        _fileWatcher?.Dispose();
        GC.SuppressFinalize(this);
    }
}
