// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Keda.Scaler.DurableTask.AzureStorage.Certificates;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Certificates;

public class CaCertificateReaderMiddlewareTests
{
    [Fact]
    public void GivenNullRequestDelegate_WhenCreatingMiddleware_ThenThrowArgumentNullException()
    {
        using ReaderWriterLockSlim readerWriterLock = new();
        _ = Assert.Throws<ArgumentNullException>(() => new CaCertificateReaderMiddleware(null!, readerWriterLock));
    }

    [Fact]
    public void GivenNullReaderWriterLock_WhenCreatingMiddleware_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => new CaCertificateReaderMiddleware(NextAsync, null!));

    [Fact]
    public async Task GivenNoOtherReaders_WhenInvokingMiddleware_ThenEnterReadLock()
    {
        using ReaderWriterLockSlim readerWriterLock = new();

        DefaultHttpContext context = new();
        CaCertificateReaderMiddleware middleware = new(NextAsync, readerWriterLock);

        Assert.False(readerWriterLock.IsReadLockHeld);
        await middleware.InvokeAsync(context);

        Assert.False(readerWriterLock.IsReadLockHeld);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task GivenOtherReader_WhenInvokingMiddleware_ThenEnterReadLock()
    {
        using ReaderWriterLockSlim readerWriterLock = new();
        using ManualResetEventSlim resetEvent = new(initialState: false);

        DefaultHttpContext context = new();
        CaCertificateReaderMiddleware middleware = new(NextAsync, readerWriterLock);

        // Start the reader
        Task readerTask = ReadAsync(readerWriterLock, resetEvent.WaitHandle);
        while (!readerWriterLock.IsReadLockHeld)
            await Task.Delay(100);

        Assert.True(readerWriterLock.IsReadLockHeld);
        await middleware.InvokeAsync(context);

        Assert.True(readerWriterLock.IsReadLockHeld);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

        resetEvent.Set();
        await readerTask;
    }

    [Fact]
    public async Task GivenWriter_WhenInvokingMiddleware_ThenWaitForWriter()
    {
        using ReaderWriterLockSlim readerWriterLock = new();
        using ManualResetEventSlim resetEvent = new(initialState: false);

        DefaultHttpContext context = new();
        CaCertificateReaderMiddleware middleware = new(NextAsync, readerWriterLock);

        // Start the writer
        Task writerTask = WriteAsync(readerWriterLock, resetEvent.WaitHandle);
        while (!readerWriterLock.IsWriteLockHeld)
            await Task.Delay(100);

        // Wait for the middleware to wait on its read
        Assert.True(readerWriterLock.IsWriteLockHeld);
        Task httpTask = middleware.InvokeAsync(context);
        while (readerWriterLock.WaitingReadCount is 0)
            await Task.Delay(100);

        resetEvent.Set();
        await Task.WhenAll(writerTask, httpTask);

        Assert.False(readerWriterLock.IsReadLockHeld);
        Assert.False(readerWriterLock.IsWriteLockHeld);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

    }

    private static Task NextAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        return Task.CompletedTask;
    }

    private static Task ReadAsync(ReaderWriterLockSlim readerWriterLock, WaitHandle waitHandle)
    {
        return Task.Run(() =>
        {
            try
            {
                readerWriterLock.EnterReadLock();
                Assert.True(waitHandle.WaitOne());
            }
            finally
            {
                readerWriterLock.ExitReadLock();
            }
        });
    }

    private static Task WriteAsync(ReaderWriterLockSlim readerWriterLock, WaitHandle waitHandle)
    {
        return Task.Run(() =>
        {
            try
            {
                readerWriterLock.EnterWriteLock();
                Assert.True(waitHandle.WaitOne());
            }
            finally
            {
                readerWriterLock.ExitWriteLock();
            }
        });
    }
}
