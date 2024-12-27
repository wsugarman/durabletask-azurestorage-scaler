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

        await middleware.InvokeAsync(context);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task GivenOtherReader_WhenInvokingMiddleware_ThenEnterReadLock()
    {
        using ReaderWriterLockSlim readerWriterLock = new();
        using ManualResetEventSlim readEvent = new(initialState: false);
        using ManualResetEventSlim resetEvent = new(initialState: false);

        DefaultHttpContext context = new();
        CaCertificateReaderMiddleware middleware = new(NextAsync, readerWriterLock);

        // Start the reader
        Task readerTask = ReadAsync(readerWriterLock, readEvent, resetEvent.WaitHandle);
        readEvent.Wait();

        await middleware.InvokeAsync(context);

        resetEvent.Set();
        await readerTask;
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task GivenWriter_WhenInvokingMiddleware_ThenWaitForWriter()
    {
        using ReaderWriterLockSlim readerWriterLock = new();
        using ManualResetEventSlim writeEvent = new(initialState: false);
        using ManualResetEventSlim resetEvent = new(initialState: false);

        DefaultHttpContext context = new();
        CaCertificateReaderMiddleware middleware = new(NextAsync, readerWriterLock);

        // Start the writer
        Task writerTask = WriteAsync(readerWriterLock, writeEvent, resetEvent.WaitHandle);
        writeEvent.Wait();

        // Wait for the middleware to wait on its read
        Task httpTask = Task.Run(() => middleware.InvokeAsync(context));
        while (readerWriterLock.WaitingReadCount is 0)
            await Task.Delay(100);

        resetEvent.Set();
        await Task.WhenAll(writerTask, httpTask);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

    }

    private static Task NextAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        return Task.CompletedTask;
    }

    private static Task ReadAsync(ReaderWriterLockSlim readerWriterLock, ManualResetEventSlim readEvent, WaitHandle waitHandle)
    {
        return Task.Run(() =>
        {
            try
            {
                readerWriterLock.EnterReadLock();
                readEvent.Set();
                Assert.True(waitHandle.WaitOne());
            }
            finally
            {
                readerWriterLock.ExitReadLock();
            }
        });
    }

    private static Task WriteAsync(ReaderWriterLockSlim readerWriterLock, ManualResetEventSlim writeEvent, WaitHandle waitHandle)
    {
        return Task.Run(() =>
        {
            try
            {
                readerWriterLock.EnterWriteLock();
                writeEvent.Set();
                Assert.True(waitHandle.WaitOne());
            }
            finally
            {
                readerWriterLock.ExitWriteLock();
            }
        });
    }
}
