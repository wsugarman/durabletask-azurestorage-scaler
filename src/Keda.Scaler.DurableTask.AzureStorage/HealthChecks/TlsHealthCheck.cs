// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Keda.Scaler.DurableTask.AzureStorage.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Keda.Scaler.DurableTask.AzureStorage.HealthChecks;

internal sealed class TlsHealthCheck : IHealthCheck
{
    private readonly CertificateFileMonitor _server;
    private readonly CertificateFileMonitor? _clientCa;

    public TlsHealthCheck(
        [FromKeyedServices("server")] CertificateFileMonitor server,
        [FromKeyedServices("clientca")] CertificateFileMonitor? clientCa = null)
    {
        ArgumentNullException.ThrowIfNull(server);

        _server = server;
        _clientCa = clientCa;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // Ensure that errors are not thrown when fetching the TLS certificates
        _ = _server.Current;
        _ = _clientCa?.Current;

        return Task.FromResult(HealthCheckResult.Healthy());
    }
}
