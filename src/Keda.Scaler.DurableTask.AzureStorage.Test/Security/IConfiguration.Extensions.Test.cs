// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Keda.Scaler.DurableTask.AzureStorage.Security;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Security;

public class IConfigurationExtensionsTest
{
    [Fact]
    public void GivenNullConfiguration_WhenCheckingMutualTlsEnforcement_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => IConfigurationExtensions.EnforceMutualTls(null!));

    [Fact]
    public void GivenDefaultConfiguration_WhenCheckingMutualTlsEnforcement_ThenReturnFalse()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        Assert.False(configuration.EnforceMutualTls());
    }

    [Theory]
    [InlineData(false, null, false)]
    [InlineData(false, "", true)]
    [InlineData(false, "  ", true)]
    [InlineData(false, "tls.crt", false)]
    [InlineData(true, "tls.crt", true)]
    public void GivenValidConfiguration_WhenCheckingMutualTlsEnforcement_ThenReturnExpectedValue(bool expected, string? certificatePath, bool validateClientCertificate)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new KeyValuePair<string, string?>[]
            {
                new("Security:Transport:Client:ValidateCertificate", validateClientCertificate.ToString()),
                new("Security:Transport:Server:CertificatePath", certificatePath),
            })
            .Build();

        Assert.Equal(expected, configuration.EnforceMutualTls());
    }

    [Fact]
    public void GivenNullConfiguration_WhenCheckingTlsEnforcement_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => IConfigurationExtensions.EnforceTls(null!));

    [Fact]
    public void GivenDefaultConfiguration_WhenCheckingTlsEnforcement_ThenReturnFalse()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        Assert.False(configuration.EnforceTls());
    }

    [Theory]
    [InlineData(false, null)]
    [InlineData(false, "")]
    [InlineData(false, "  ")]
    [InlineData(true, "tls.crt")]
    public void GivenValidConfiguration_WhenCheckingTlsEnforcement_ThenReturnExpectedValue(bool expected, string? certificatePath)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new KeyValuePair<string, string?>[]
            {
                new("Security:Transport:Server:CertificatePath", certificatePath),
            })
            .Build();

        Assert.Equal(expected, configuration.EnforceTls());
    }

    [Fact]
    public void GivenNullConfiguration_WhenCheckingCustomClientCa_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => IConfigurationExtensions.UseCustomClientCa(null!));

    [Fact]
    public void GivenDefaultConfiguration_WhenCheckingCustomClientCa_ThenReturnFalse()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        Assert.False(configuration.UseCustomClientCa());
    }

    [Theory]
    [InlineData(false, null)]
    [InlineData(false, "")]
    [InlineData(false, "  ")]
    [InlineData(true, "tls.crt")]
    public void GivenValidConfiguration_WhenCheckingCustomClientCa_ThenReturnExpectedValue(bool expected, string? certificatePath)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new KeyValuePair<string, string?>[]
            {
                new("Security:Transport:Client:CaCertificatePath", certificatePath),
            })
            .Build();

        Assert.Equal(expected, configuration.UseCustomClientCa());
    }
}
