// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.Clients;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Clients;

public class ValidateAzureStorageAccountOptionsTest
{
    private readonly ScalerMetadata _metadata = new();
    private readonly ValidateAzureStorageAccountOptions _validate;

    public ValidateAzureStorageAccountOptionsTest()
    {
        IScalerMetadataAccessor scalerMetadataAccessor = Substitute.For<IScalerMetadataAccessor>();
        _ = scalerMetadataAccessor.ScalerMetadata.Returns(_metadata);
        _validate = new(scalerMetadataAccessor);
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    [InlineData("123-456-789")]
    public void GivenConnectionStringMetadataWithClientId_WhenValidatingAzureStorageAccountOptions_ThenReturnFailure(string clientId)
        => GivenConnectionStringMetadataWithUriProperty_WhenValidatingAzureStorageAccountOptions_ThenReturnFailure(m => m.ClientId = clientId, nameof(ScalerMetadata.ClientId));

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    [InlineData(CloudEnvironment.AzurePublicCloud)]
    public void GivenConnectionStringMetadataWithCloud_WhenValidatingAzureStorageAccountOptions_ThenReturnFailure(string cloud)
        => GivenConnectionStringMetadataWithUriProperty_WhenValidatingAzureStorageAccountOptions_ThenReturnFailure(m => m.Cloud = cloud, nameof(ScalerMetadata.Cloud));

    private void GivenConnectionStringMetadataWithUriProperty_WhenValidatingAzureStorageAccountOptions_ThenReturnFailure(Action<ScalerMetadata> configure, string name)
    {
        ArgumentNullException.ThrowIfNull(configure);

        const string ConnectionString = "foo=bar";

        _metadata.Connection = ConnectionString;
        configure(_metadata);

        AzureStorageAccountOptions options = new() { ConnectionString = ConnectionString };
        ValidateOptionsResult result = _validate.Validate(Options.DefaultName, options);

        Assert.False(result.Succeeded);
        Assert.True(result.Failed);

        string failureMessage = Assert.Single(result.Failures);
        Assert.Contains(name, failureMessage, StringComparison.Ordinal);
    }
}
