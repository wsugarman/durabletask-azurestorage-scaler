// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.Clients;
using Keda.Scaler.DurableTask.AzureStorage.Metadata;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Clients;

[TestClass]
public class ValidateAzureStorageAccountOptionsTest
{
    private readonly ScalerOptions _scalerOptions = new();
    private readonly ConfigureAzureStorageAccountOptions _configure;
    private readonly ValidateAzureStorageAccountOptions _validate;

    public ValidateAzureStorageAccountOptionsTest()
    {
        IOptionsSnapshot<ScalerOptions> snapshot = Substitute.For<IOptionsSnapshot<ScalerOptions>>();
        _ = snapshot.Get(default).Returns(_scalerOptions);
        _configure = new(snapshot);
        _validate = new(snapshot);
    }

    [TestMethod]
    public void GivenNullOptionsSnapshot_WhenCreatingValidate_ThenThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ValidateAzureStorageAccountOptions(null!));

        IOptionsSnapshot<ScalerOptions> nullSnapshot = Substitute.For<IOptionsSnapshot<ScalerOptions>>();
        _ = nullSnapshot.Get(default).Returns(default(ScalerOptions));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ValidateAzureStorageAccountOptions(nullSnapshot));
    }

    [TestMethod]
    [DataRow("ExampleEnvVariable")]
    [DataRow(null)]
    [DoNotParallelize]
    public void GivenUnresolvedConnectionString_WhenValidatingOptions_ThenReturnFailure(string? variableName)
    {
        string envVariable = variableName ?? AzureStorageAccountOptions.DefaultConnectionEnvironmentVariable;
        using IDisposable env = TestEnvironment.SetVariable(envVariable, null);

        AzureStorageAccountOptions options = new();
        _scalerOptions.ConnectionFromEnv = variableName;
        _configure.Configure(options);

        ValidateOptionsResult result = _validate.Validate(Options.DefaultName, options);

        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(result.Failed);

        string failureMessage = Assert.ContainsSingle(result.Failures);
        Assert.Contains(envVariable, failureMessage, StringComparison.Ordinal);
    }

    [TestMethod]
    public void GivenValidConnectionString_WhenValidatingOptions_ThenReturnSuccess()
        => GivenValidCombination_WhenValidatingOptions_ThenReturnSuccess(o => o.Connection = "foo=bar");

    [TestMethod]
    public void GivenValidAccountName_WhenValidatingOptions_ThenReturnSuccess()
        => GivenValidCombination_WhenValidatingOptions_ThenReturnSuccess(o => o.AccountName = "unittest");

    private void GivenValidCombination_WhenValidatingOptions_ThenReturnSuccess(Action<ScalerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        AzureStorageAccountOptions options = new();
        configure(_scalerOptions);
        _configure.Configure(options);

        ValidateOptionsResult result = _validate.Validate(Options.DefaultName, options);

        Assert.IsTrue(result.Succeeded);
        Assert.IsFalse(result.Failed);
        Assert.IsNull(result.Failures);
    }
}
