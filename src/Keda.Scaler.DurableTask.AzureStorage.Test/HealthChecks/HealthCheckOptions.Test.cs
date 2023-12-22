// Copyright © William Sugarman.
// Licensed under the MIT License.

using Keda.Scaler.DurableTask.AzureStorage.HealthChecks;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.HealthChecks;

public class HealthCheckOptionsTest
{
    [Theory]
    [InlineData(0)]
    [InlineData(1023)]
    [InlineData(ushort.MaxValue + 1)]
    public void GivenInvalidPort_WhenValidatingHealthCheckOptions_ThenFailValidation(int port)
    {
        HealthCheckOptions options = new() { Port = port };
        Assert.True(new ValidateHealthCheckOptions().Validate(null, options).Failed);
    }
}
