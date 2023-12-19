// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Keda.Scaler.DurableTask.AzureStorage.Security;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Security;

internal static class CertificateFileMonitorExtensions
{
    public static async Task WaitForThumbprintAsync(
        this CertificateFileMonitor monitor,
        string expected,
        TimeSpan pollingInterval,
        CancellationToken cancellationToken = default)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (string.Equals(monitor.Current.Thumbprint, expected, StringComparison.Ordinal))
                    break;
            }
            catch (CryptographicException)
            { }

            await Task.Delay(pollingInterval, cancellationToken);
        }
    }

    public static async Task WaitForExceptionAsync(
        this CertificateFileMonitor monitor,
        TimeSpan pollingInterval,
        CancellationToken cancellationToken = default)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                _ = monitor.Current;
            }
            catch (CryptographicException)
            {
                return;
            }

            await Task.Delay(pollingInterval, cancellationToken);
        }
    }
}
