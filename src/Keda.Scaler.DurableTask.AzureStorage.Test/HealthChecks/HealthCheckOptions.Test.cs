// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Net;
using Keda.Scaler.DurableTask.AzureStorage.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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
    [InlineData(true, "127.0.0.1", 1234)]
    [InlineData(false, "1.2.3.4", 1234)]
    [InlineData(false, "127.0.0.1", 6789)]
    public void GivenInvalidHttpContext_WhenCheckingIfHealthCheckRequest_ThenReturnFalse(bool isHttps, string remoteAddress, int port)
    {
        HealthCheckOptions options = new() { Port = 1234 };
        DefaultHttpContext context = new()
        {
            Request = { IsHttps = isHttps },
            Connection =
            {
                LocalPort = port,
                RemoteIpAddress = IPAddress.Parse(remoteAddress),
            },
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
        };

        Assert.False(options.IsHealthCheckRequest(context));
    }

    [Fact]
    public void GivenValidHttpContext_WhenCheckingIfHealthCheckRequest_ThenReturnTrue()
    {
        HealthCheckOptions options = new() { Port = 1234 };
        DefaultHttpContext context = new()
        {
            Request = { IsHttps = false },
            Connection =
            {
                LocalPort = 1234,
                RemoteIpAddress = IPAddress.Loopback,
            },
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
        };

        Assert.True(options.IsHealthCheckRequest(context));
    }
}
