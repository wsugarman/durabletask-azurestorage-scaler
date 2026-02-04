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
    public void GivenNullConfiguration_WhenCheckingTlsEnforcement_ThenThrowArgumentNullException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => IConfigurationExtensions.IsTlsEnforced(null!));

    [TestMethod]
    public void GivenDefaultConfiguration_WhenCheckingTlsEnforcement_ThenReturnfalse()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        Assert.IsFalse(configuration.IsTlsEnforced());
    }

    [TestMethod]
    [DataRow(false, null)]
    [DataRow(false, "")]
    [DataRow(false, "  ")]
    [DataRow(true, "tls.crt")]
    public void GivenValidConfiguration_WhenCheckingTlsEnforcement_ThenReturnExpectedValue(bool expected, string? certificatePath)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new("Kestrel:Certificates:Default:Path", certificatePath)])
            .Build();

        Assert.AreEqual(expected, configuration.IsTlsEnforced());
    }

    [TestMethod]
    public void GivenNullConfiguration_WhenCheckingCustomClientCa_ThenThrowArgumentNullException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => IConfigurationExtensions.UseCustomClientCa(null!));

    [TestMethod]
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

        _ = Assert.ThrowsExactly<OptionsValidationException>(() => configuration.UseCustomClientCa());
    }

    [TestMethod]
    public void GivenDefaultConfiguration_WhenCheckingCustomClientCa_ThenReturnfalse()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        Assert.IsFalse(configuration.UseCustomClientCa());
    }

    [TestMethod]
    [DataRow(false, null, false, false)]
    [DataRow(false, "tls.crt", false, true)]
    [DataRow(false, "tls.crt", true, false)]
    [DataRow(true, "tls.crt", true, true)]
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
        Assert.AreEqual(expected, configuration.UseCustomClientCa());
    }

    [TestMethod]
    public void GivenNullConfiguration_WhenCheckingClientCertificateValidation_ThenThrowArgumentNullException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => IConfigurationExtensions.ValidateClientCertificate(null!));

    [TestMethod]
    public void GivenDefaultConfiguration_WhenCheckingClientCertificateValidation_ThenReturnfalse()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        Assert.IsFalse(configuration.ValidateClientCertificate());
    }

    [TestMethod]
    [DataRow(false, null, false)]
    [DataRow(false, null, true)]
    [DataRow(false, "tls.crt", false)]
    [DataRow(true, "tls.crt", true)]
    public void GivenValidConfiguration_WhenCheckingClientCertificateValidation_ThenReturnExpectedValue(bool expected, string? certificatePath, bool validate)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new("Kestrel:Certificates:Default:Path", certificatePath),
                new($"{ClientCertificateValidationOptions.DefaultKey}:{nameof(ClientCertificateValidationOptions.Enable)}", validate.ToString()),
            ])
            .Build();

        Assert.AreEqual(expected, configuration.ValidateClientCertificate());
    }
}
