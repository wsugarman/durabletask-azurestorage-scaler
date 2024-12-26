// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.Certificates;
using Microsoft.Extensions.Configuration;
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

    [Theory]
    [InlineData(false, null, false, null)]
    [InlineData(false, "", true, "ca.crt")]
    [InlineData(false, "  ", true, "ca.crt")]
    [InlineData(false, "tls.crt", false, "ca.crt")]
    [InlineData(true, "tls.crt", true, null)]
    [InlineData(true, "tls.crt", true, "ca.crt")]
    public void GivenValidConfiguration_WhenCheckingCustomClientCa_ThenReturnExpectedValue(bool expected, string? certificatePath, bool validate, string? caPath)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new("Kestrel:Certificates:Default:Path", certificatePath),
                new("Kestrel:Client:Certificate:Validation:CertificateAuthority:Path", caPath),
                new("Kestrel:Client:Certificate:Validation:Enabled", validate.ToString()),
            ])
            .Build();

        Assert.Equal(expected, configuration.UseCustomClientCa());
    }

    [Theory]
    [InlineData(false, null, false)]
    [InlineData(false, "", true)]
    [InlineData(false, "  ", true)]
    [InlineData(false, "tls.crt", false)]
    [InlineData(true, "tls.crt", true)]
    public void GivenValidConfiguration_WhenCheckingClientCertificateValidation_ThenReturnExpectedValue(bool expected, string? certificatePath, bool validate)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new("Kestrel:Certificates:Default:Path", certificatePath),
                new("Kestrel:Client:Certificate:Validation:Enabled", validate.ToString()),
            ])
            .Build();

        Assert.Equal(expected, configuration.ValidateClientCertificate());
    }
}
