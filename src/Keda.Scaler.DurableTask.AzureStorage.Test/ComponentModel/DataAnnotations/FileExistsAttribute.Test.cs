// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Keda.Scaler.DurableTask.AzureStorage.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.ComponentModel.DataAnnotations;

public sealed class FileExistsAttributeTests : IDisposable
{
    private readonly string _tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public FileExistsAttributeTests()
        => Directory.CreateDirectory(_tempPath);

    [Fact]
    public void GivenIncorrectMemberType_WhenValidatingFileExists_ThenThrowOptionsValidationException()
    {
        ServiceCollection services = new();
        _ = services
            .AddOptions<InvalidExampleOptions>()
            .Configure(o => o.Number = 42)
            .ValidateDataAnnotations();

        using ServiceProvider provider = services.BuildServiceProvider();
        _ = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<InvalidExampleOptions>>().Value);
    }

    [Fact]
    public void GivenMissingFile_WhenValidatingFileExists_ThenThrowOptionsValidationException()
    {
        ServiceCollection services = new();
        _ = services
            .AddOptions<ExampleOptions>()
            .Configure(o => o.FilePath = Path.Combine(_tempPath, Guid.NewGuid().ToString()))
            .ValidateDataAnnotations();

        using ServiceProvider provider = services.BuildServiceProvider();
        _ = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<ExampleOptions>>().Value);
    }

    [Fact]
    public void GivenPresentFile_WhenValidatingFileExists_ThenDoNotThrowException()
    {
        string filePath = Path.Combine(_tempPath, "file.txt");
        File.WriteAllText(filePath, "Hello, World!");

        ServiceCollection services = new();
        _ = services
            .AddOptions<ExampleOptions>()
            .Configure(o => o.FilePath = filePath)
            .ValidateDataAnnotations();

        using ServiceProvider provider = services.BuildServiceProvider();
        ExampleOptions actual = provider.GetRequiredService<IOptions<ExampleOptions>>().Value;
        Assert.Equal(filePath, actual.FilePath);
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
