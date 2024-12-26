// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Certificates;

public sealed class TestDirectory : IAsyncLifetime
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());

    public Task InitializeAsync()
    {
        _ = Directory.CreateDirectory(Path);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        Directory.Delete(Path, recursive: true);
        return Task.CompletedTask;
    }
}
