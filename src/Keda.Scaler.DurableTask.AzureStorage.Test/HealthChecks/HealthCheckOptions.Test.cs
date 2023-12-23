// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.HealthChecks;
using Microsoft.AspNetCore.Http;
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

    [Fact]
    public void GivenValidPort_WhenValidatingHealthCheckOptions_ThenSucceedValidation()
    {
        HealthCheckOptions options = new() { Port = 1234 };
        Assert.True(new ValidateHealthCheckOptions().Validate(null, options).Succeeded);
    }

    [Fact]
    public void GivenNullHttpContext_WhenCheckingIfHealthCheckRequest_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new HealthCheckOptions().IsHealthCheckRequest(null!));

    [Theory]
    [InlineData(false, 6789)]
    [InlineData(true, 1234)]
    public void GivenInvalidHttpContext_WhenCheckingIfHealthCheckRequest_ThenReturnFalse(bool isHttps, int port)
    {
        HealthCheckOptions options = new() { Port = 1234 };
        DefaultHttpContext context = new()
        {
            Connection = { LocalPort = port },
            Request = { IsHttps = isHttps },
        };

        Assert.False(options.IsHealthCheckRequest(context));
    }

    [Fact]
    public void GivenValidHttpContext_WhenCheckingIfHealthCheckRequest_ThenReturnTrue()
    {
        HealthCheckOptions options = new() { Port = 1234 };
        DefaultHttpContext context = new()
        {
            Connection = { LocalPort = 1234 },
            Request = { IsHttps = false },
        };

        Assert.True(options.IsHealthCheckRequest(context));
    }
}
