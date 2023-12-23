// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Keda.Scaler.DurableTask.AzureStorage.HealthChecks;

internal sealed class HealthCheckOptions
{
    public const string DefaultKey = "HealthCheck";

    [Range(1024, ushort.MaxValue)]
    public int Port { get; set; }

    public bool IsHealthCheckRequest(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return !context.Request.IsHttps && context.Connection.LocalPort == Port;
    }
}
