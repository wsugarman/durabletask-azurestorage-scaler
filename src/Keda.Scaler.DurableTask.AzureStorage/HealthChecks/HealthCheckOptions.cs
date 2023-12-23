// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Keda.Scaler.DurableTask.AzureStorage.HealthChecks;

internal sealed class HealthCheckOptions
{
    public const string DefaultKey = "HealthCheck";

    [Range(1024, ushort.MaxValue)]
    public int Port { get; set; }

    public bool IsHealthCheckRequest(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ILogger logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(LogCategories.Default);
#pragma warning disable CA1848 // Use the LoggerMessage delegates
        logger.LogInformation(
            "Received request from '{RemoteAddress}:{RemotePort}' for {LocalAddress}:{LocalPort}. Https? {IsHttps}",
            context.Connection.RemoteIpAddress,
            context.Connection.RemotePort,
            context.Connection.LocalIpAddress,
            context.Connection.LocalPort,
            context.Request.IsHttps);
#pragma warning restore CA1848 // Use the LoggerMessage delegates

        return !context.Request.IsHttps
            && IPAddress.IsLoopback(context.Connection.RemoteIpAddress!)
            && context.Connection.LocalPort == Port;
    }
}
