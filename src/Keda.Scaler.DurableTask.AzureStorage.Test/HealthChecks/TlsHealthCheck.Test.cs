// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Keda.Scaler.DurableTask.AzureStorage.HealthChecks;
using Keda.Scaler.DurableTask.AzureStorage.Test.Security;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;
using Xunit.Abstractions;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.HealthChecks;

public class TlsHealthCheckTest(ITestOutputHelper outputHelper) : TlsCertificateTest(outputHelper)
{
    [Fact]
    public void GivenNullServerCertificate_WhenCreatingTlsHealthCheck_ThenThrowArgumentNullException()
    {
        _ = Assert.Throws<ArgumentNullException>(() => new TlsHealthCheck(null!));
        _ = Assert.Throws<ArgumentNullException>(() => new TlsHealthCheck(null!, ClientCa));
    }

    [Fact]
    public void GivenNullClientCaCertificate_WhenCreatingTlsHealthCheck_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new TlsHealthCheck(Server, null!));

    [Fact]
    public async Task GivenInvalidServerCertificate_WhenCheckingHealth_ThenThrowException()
    {
        using CancellationTokenSource tokenSource = new(TimeSpan.FromSeconds(15));
        TlsHealthCheck healthCheck = new(Server);

        // First check that the certificate can be successfully read
        HealthCheckResult actual = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
        Assert.Equal(HealthStatus.Healthy, actual.Status);

        // Now invalidate the certificate
        await File.WriteAllTextAsync(ServerCertPath, "Invalid", tokenSource.Token);
        await Server.WaitForExceptionAsync(TimeSpan.FromMilliseconds(500), tokenSource.Token);

        _ = await Assert.ThrowsAnyAsync<CryptographicException>(() => healthCheck.CheckHealthAsync(new HealthCheckContext(), tokenSource.Token));
    }

    [Fact]
    public async Task GivenInvalidClientCaCertificate_WhenCheckingHealth_ThenThrowException()
    {
        using CancellationTokenSource tokenSource = new(TimeSpan.FromSeconds(15));
        TlsHealthCheck healthCheck = new(Server, ClientCa);

        // First check that the certificate can be successfully read
        HealthCheckResult actual = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
        Assert.Equal(HealthStatus.Healthy, actual.Status);

        // Now invalidate the certificate
        await File.WriteAllTextAsync(CaCertPath, "Invalid", tokenSource.Token);
        await ClientCa.WaitForExceptionAsync(TimeSpan.FromMilliseconds(500), tokenSource.Token);

        _ = await Assert.ThrowsAnyAsync<CryptographicException>(() => healthCheck.CheckHealthAsync(new HealthCheckContext(), tokenSource.Token));
    }

    [Fact]
    public async Task GivenValidServerCertificate_WhenCheckingHealth_ThenReturnHealthy()
    {
        TlsHealthCheck healthCheck = new(Server);

        HealthCheckResult actual = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
        Assert.Equal(HealthStatus.Healthy, actual.Status);
    }

    [Fact]
    public async Task GivenValidCertificates_WhenCheckingHealth_ThenReturnHealthy()
    {
        TlsHealthCheck healthCheck = new(Server, ClientCa);

        HealthCheckResult actual = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
        Assert.Equal(HealthStatus.Healthy, actual.Status);
    }
}
