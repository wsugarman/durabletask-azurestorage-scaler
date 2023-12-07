// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Keda.Scaler.DurableTask.AzureStorage.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.DataAnnotations;

[TestClass]
public sealed class FileExistsAttributeTests : IDisposable
{
    private readonly string _tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public FileExistsAttributeTests()
        => Directory.CreateDirectory(_tempPath);

    [TestMethod]
    public void IsValid()
    {
        string filePath = Path.Combine(_tempPath, "file.txt");
        File.WriteAllText(filePath, "Hello, World!");

        ServiceCollection services = new();
        _ = services
            .AddOptions<InvalidExampleOptions>()
            .Configure(o => o.Number = 42)
            .ValidateDataAnnotations();

        _ = services
            .AddOptions<ExampleOptions>("Bad")
            .Configure(o => o.FilePath = Path.Combine(_tempPath, Guid.NewGuid().ToString()))
            .ValidateDataAnnotations();

        _ = services
            .AddOptions<ExampleOptions>("Good")
            .Configure(o => o.FilePath = filePath)
            .ValidateDataAnnotations();

        IServiceProvider provider = services.BuildServiceProvider();

        // Wrong type
        _ = Assert.ThrowsException<OptionsValidationException>(() => provider.GetRequiredService<IOptions<InvalidExampleOptions>>().Value);

        // File not found
        _ = Assert.ThrowsException<OptionsValidationException>(() => provider.GetRequiredService<IOptionsSnapshot<ExampleOptions>>().Get("Bad"));

        // Valid file
        ExampleOptions actual = provider.GetRequiredService<IOptionsSnapshot<ExampleOptions>>().Get("Good");
        Assert.AreEqual(filePath, actual.FilePath);
    }

    public void Dispose()
        => Directory.Delete(_tempPath, true);

    [SuppressMessage("Microsoft.Performance", "CA1812:Avoid uninstantiated internal classes.", Justification = "Initialized via dependency injection.")]
    private sealed class InvalidExampleOptions
    {
        [FileExists]
        public int Number { get; set; } = 1;
    }

    [SuppressMessage("Microsoft.Performance", "CA1812:Avoid uninstantiated internal classes.", Justification = "Initialized via dependency injection.")]
    private sealed class ExampleOptions
    {
        [FileExists]
        public string? FilePath { get; set; }
    }
}
