// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Keda.Scaler.DurableTask.AzureStorage.Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Certificates;

public class IConfigurationExtensionsTest
{
    [Fact]
    public void GivenNullConfiguration_WhenCheckingTlsEnforcement_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => IConfigurationExtensions.IsTlsEnforced(null!));

    [Fact]
    public void GivenDefaultConfiguration_WhenCheckingTlsEnforcement_ThenReturnfalse()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        Assert.False(configuration.IsTlsEnforced());
    }

    [Theory]
    [InlineData(false, null)]
    [InlineData(false, "")]
    [InlineData(false, "  ")]
    [InlineData(true, "tls.crt")]
    public void GivenValidConfiguration_WhenCheckingTlsEnforcement_ThenReturnExpectedValue(bool expected, string? certificatePath)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new("Kestrel:Certificates:Default:Path", certificatePath)])
            .Build();

        Assert.Equal(expected, configuration.IsTlsEnforced());
    }

    [Fact]
    public void GivenNullConfiguration_WhenCheckingCustomClientCa_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => IConfigurationExtensions.UseCustomClientCa(null!));

    [Fact]
    public void GivenInvalidConfiguration_WhenCheckingCustomClientCa_ThenThrowOptionsValidationException()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new("Kestrel:Certificates:Default:Path", "tls.crt"),
                new($"{ClientCertificateValidationOptions.DefaultKey}:{nameof(ClientCertificateValidationOptions.Enable)}", "true"),
                new($"{ClientCertificateValidationOptions.DefaultKey}:{nameof(ClientCertificateValidationOptions.CertificateAuthority)}:{nameof(CaCertificateFileOptions.Path)}", ""),
            ])
            .Build();

        _ = Assert.Throws<OptionsValidationException>(() => configuration.UseCustomClientCa());
    }

    [Fact]
    public void GivenDefaultConfiguration_WhenCheckingCustomClientCa_ThenReturnfalse()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        Assert.False(configuration.UseCustomClientCa());
    }

    [Theory]
    [InlineData(false, null, false, false)]
    [InlineData(false, "tls.crt", false, true)]
    [InlineData(false, "tls.crt", true, false)]
    [InlineData(true, "tls.crt", true, true)]
    public void GivenValidConfiguration_WhenCheckingCustomClientCa_ThenReturnExpectedValue(bool expected, string? certificatePath, bool validate, bool includeCa)
    {
        Dictionary<string, string?> pairs = new()
        {
            { "Kestrel:Certificates:Default:Path", certificatePath },
            { $"{ClientCertificateValidationOptions.DefaultKey}:{nameof(ClientCertificateValidationOptions.Enable)}", validate.ToString() },
        };

        if (includeCa)
            pairs.Add($"{ClientCertificateValidationOptions.DefaultKey}:{nameof(ClientCertificateValidationOptions.CertificateAuthority)}:{nameof(CaCertificateFileOptions.Path)}", "ca.crt");

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(pairs).Build();
        Assert.Equal(expected, configuration.UseCustomClientCa());
    }

    [Fact]
    public void GivenNullConfiguration_WhenCheckingClientCertificateValidation_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => IConfigurationExtensions.ValidateClientCertificate(null!));

    [Fact]
    public void GivenDefaultConfiguration_WhenCheckingClientCertificateValidation_ThenReturnfalse()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        Assert.False(configuration.ValidateClientCertificate());
    }

    [Theory]
    [InlineData(false, null, false)]
    [InlineData(false, null, true)]
    [InlineData(false, "tls.crt", false)]
    [InlineData(true, "tls.crt", true)]
    public void GivenValidConfiguration_WhenCheckingClientCertificateValidation_ThenReturnExpectedValue(bool expected, string? certificatePath, bool validate)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new("Kestrel:Certificates:Default:Path", certificatePath),
                new($"{ClientCertificateValidationOptions.DefaultKey}:{nameof(ClientCertificateValidationOptions.Enable)}", validate.ToString()),
            ])
            .Build();

        Assert.Equal(expected, configuration.ValidateClientCertificate());
    }
}
