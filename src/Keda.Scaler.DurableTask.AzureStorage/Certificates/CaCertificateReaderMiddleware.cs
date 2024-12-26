// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Keda.Scaler.DurableTask.AzureStorage.Certificates;

internal sealed class CaCertificateReaderMiddleware(RequestDelegate next, ReaderWriterLockSlim certificateLock)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly ReaderWriterLockSlim _certificateLock = certificateLock ?? throw new ArgumentNullException(nameof(certificateLock));

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            _certificateLock.EnterReadLock();
            await _next(context);
        }
        finally
        {
            _certificateLock.ExitReadLock();
        }
    }
}
