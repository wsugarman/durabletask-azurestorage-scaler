// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Keda.Scaler.DurableTask.AzureStorage.Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Certificates;

[TestClass]
public class IConfigurationExtensionsTest
{
    [TestMethod]
    public void GivenNullConfiguration_WhenCheckingWhetherTlsIsEnabled_ThenThrowArgumentNullException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => IConfigurationExtensions.IsTlsEnabled(null!));

    [TestMethod]
    public void GivenDefaultConfiguration_WhenCheckingWhetherTlsIsEnabled_ThenReturnfalse()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        Assert.IsFalse(configuration.IsTlsEnabled());
    }

    [TestMethod]
    [DataRow(false, null)]
    [DataRow(false, "")]
    [DataRow(false, "  ")]
    [DataRow(true, "tls.crt")]
    public void GivenValidConfiguration_WhenCheckingWhetherTlsIsEnabled_ThenReturnExpectedValue(bool expected, string? certificatePath)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new("Kestrel:Certificates:Default:Path", certificatePath)])
            .Build();

        Assert.AreEqual(expected, configuration.IsTlsEnabled());
    }

    [TestMethod]
    public void GivenNullConfiguration_WhenCheckingWhetherCustomClientCaIsConfigured_ThenThrowArgumentNullException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => IConfigurationExtensions.IsCustomClientCaConfigured(null!));

    [TestMethod]
    public void GivenInvalidConfiguration_WhenCheckingWhetherCustomClientCaIsConfigured_ThenThrowOptionsValidationException()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new("Kestrel:Certificates:Default:Path", "tls.crt"),
                new($"{ClientCertificateValidationOptions.DefaultKey}:{nameof(ClientCertificateValidationOptions.Enable)}", "true"),
                new($"{ClientCertificateValidationOptions.DefaultKey}:{nameof(ClientCertificateValidationOptions.CertificateAuthority)}:{nameof(CaCertificateFileOptions.Path)}", ""),
            ])
            .Build();

        _ = Assert.ThrowsExactly<OptionsValidationException>(() => configuration.IsCustomClientCaConfigured());
    }

    [TestMethod]
    public void GivenDefaultConfiguration_WhenCheckingWhetherCustomClientCaIsConfigured_ThenReturnfalse()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        Assert.IsFalse(configuration.IsCustomClientCaConfigured());
    }

    [TestMethod]
    [DataRow(false, null, false, false)]
    [DataRow(false, "tls.crt", false, true)]
    [DataRow(false, "tls.crt", true, false)]
    [DataRow(true, "tls.crt", true, true)]
    public void GivenValidConfiguration_WhenCheckingWhetherCustomClientCaIsConfigured_ThenReturnExpectedValue(bool expected, string? certificatePath, bool validate, bool includeCa)
    {
        Dictionary<string, string?> pairs = new()
        {
            { "Kestrel:Certificates:Default:Path", certificatePath },
            { $"{ClientCertificateValidationOptions.DefaultKey}:{nameof(ClientCertificateValidationOptions.Enable)}", validate.ToString() },
        };

        if (includeCa)
            pairs.Add($"{ClientCertificateValidationOptions.DefaultKey}:{nameof(ClientCertificateValidationOptions.CertificateAuthority)}:{nameof(CaCertificateFileOptions.Path)}", "ca.crt");

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(pairs).Build();
        Assert.AreEqual(expected, configuration.IsCustomClientCaConfigured());
    }

    [TestMethod]
    public void GivenNullConfiguration_WhenCheckingWhetherClientCertificateShouldBeValidated_ThenThrowArgumentNullException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => IConfigurationExtensions.ShouldValidateClientCertificate(null!));

    [TestMethod]
    public void GivenDefaultConfiguration_WhenCheckingWhetherClientCertificateShouldBeValidated_ThenReturnfalse()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        Assert.IsFalse(configuration.ShouldValidateClientCertificate());
    }

    [TestMethod]
    [DataRow(false, null, false)]
    [DataRow(false, null, true)]
    [DataRow(false, "tls.crt", false)]
    [DataRow(true, "tls.crt", true)]
    public void GivenValidConfiguration_WhenCheckingWhetherClientCertificateShouldBeValidated_ThenReturnExpectedValue(bool expected, string? certificatePath, bool validate)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new("Kestrel:Certificates:Default:Path", certificatePath),
                new($"{ClientCertificateValidationOptions.DefaultKey}:{nameof(ClientCertificateValidationOptions.Enable)}", validate.ToString()),
            ])
            .Build();

        Assert.AreEqual(expected, configuration.ShouldValidateClientCertificate());
    }
}
