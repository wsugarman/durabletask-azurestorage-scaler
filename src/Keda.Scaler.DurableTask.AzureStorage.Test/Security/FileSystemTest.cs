// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.IO;
using Keda.Scaler.DurableTask.AzureStorage.Test.Logging;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Security;

public class FileSystemTest : IDisposable
{
    private bool _disposed;

    public ILoggerFactory LoggerFactory { get; }

    public ILogger Logger { get; }

    public string RootFolder { get; } = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public FileSystemTest(ITestOutputHelper outputHelper)
    {
        LoggerFactory = XUnitLogger.CreateFactory(outputHelper);
        Logger = LoggerFactory.CreateLogger(LogCategories.Security);
        _ = Directory.CreateDirectory(RootFolder);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
                Directory.Delete(RootFolder, recursive: true);

            _disposed = true;
        }
    }
}
