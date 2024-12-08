// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.Clients;
using Keda.Scaler.DurableTask.AzureStorage.Metadata;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Clients;

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

    [Fact]
    public void GivenNullOptionsSnapshot_WhenCreatingValidate_ThenThrowArgumentNullException()
    {
        _ = Assert.Throws<ArgumentNullException>(() => new ValidateAzureStorageAccountOptions(null!));

        IOptionsSnapshot<ScalerOptions> nullSnapshot = Substitute.For<IOptionsSnapshot<ScalerOptions>>();
        _ = nullSnapshot.Get(default).Returns(default(ScalerOptions));
        _ = Assert.Throws<ArgumentNullException>(() => new ValidateAzureStorageAccountOptions(nullSnapshot));
    }

    [Theory]
    [InlineData("ExampleEnvVariable")]
    [InlineData(null)]
    public void GivenUnresolvedConnectionString_WhenValidatingOptions_ThenReturnFailure(string? variableName)
    {
        GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(
            variableName ?? AzureStorageAccountOptions.DefaultConnectionEnvironmentVariable,
            o => o.ConnectionFromEnv = variableName);
    }

    [Fact]
    public void GivenUnresolvedCloud_WhenValidatingOptions_ThenReturnFailure()
    {
        GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(
            nameof(ScalerOptions.Cloud),
            o =>
            {
                o.AccountName = "unittest";
                o.Cloud = "unknown";
                o.UseManagedIdentity = true;
            });
    }

    private void GivenInvalidCombination_WhenValidatingOptions_ThenReturnFailure(string failureSnippet, Action<ScalerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        AzureStorageAccountOptions options = new();
        configure(_scalerOptions);
        _configure.Configure(options);

        ValidateOptionsResult result = _validate.Validate(Options.DefaultName, options);

        Assert.False(result.Succeeded);
        Assert.True(result.Failed);

        string failureMessage = Assert.Single(result.Failures);
        Assert.Contains(failureSnippet, failureMessage, StringComparison.Ordinal);
    }
}
