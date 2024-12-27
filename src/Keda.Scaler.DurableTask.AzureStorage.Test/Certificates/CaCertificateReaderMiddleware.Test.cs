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
    public async ValueTask GivenNoOtherReaders_WhenInvokingMiddleware_ThenEnterReadLock()
    {
        using ReaderWriterLockSlim readerWriterLock = new();

        DefaultHttpContext context = new();
        CaCertificateReaderMiddleware middleware = new(NextAsync, readerWriterLock);

        await middleware.InvokeAsync(context);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async ValueTask GivenOtherReader_WhenInvokingMiddleware_ThenEnterReadLock()
    {
        using ReaderWriterLockSlim readerWriterLock = new();
        using ManualResetEventSlim readEvent = new(initialState: false);
        using ManualResetEventSlim middlewareEvent = new(initialState: false);

        DefaultHttpContext context = new() { RequestAborted = TestContext.Current.CancellationToken };
        CaCertificateReaderMiddleware middleware = new(NextAsync, readerWriterLock);

        // Start the reader
        Task readerTask = ReadAsync(readerWriterLock, readEvent, middlewareEvent, TestContext.Current.CancellationToken);
        readEvent.Wait(TestContext.Current.CancellationToken);

        await middleware.InvokeAsync(context);

        middlewareEvent.Set();
        await readerTask;
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async ValueTask GivenWriter_WhenInvokingMiddleware_ThenWaitForWriter()
    {
        using ReaderWriterLockSlim readerWriterLock = new();
        using ManualResetEventSlim writeEvent = new(initialState: false);
        using ManualResetEventSlim middlewareEvent = new(initialState: false);

        DefaultHttpContext context = new() { RequestAborted = TestContext.Current.CancellationToken };
        CaCertificateReaderMiddleware middleware = new(NextAsync, readerWriterLock);

        // Start the writer
        Task writerTask = WriteAsync(readerWriterLock, writeEvent, middlewareEvent, TestContext.Current.CancellationToken);
        writeEvent.Wait(TestContext.Current.CancellationToken);

        // Wait for the middleware to wait on its read
        Task httpTask = Task.Run(() => middleware.InvokeAsync(context), TestContext.Current.CancellationToken);
        while (readerWriterLock.WaitingReadCount is 0)
            await Task.Delay(100, TestContext.Current.CancellationToken);

        middlewareEvent.Set();
        await Task.WhenAll(writerTask, httpTask);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

    }

    private static Task NextAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        return Task.CompletedTask;
    }

    private static Task ReadAsync(ReaderWriterLockSlim readerWriterLock, ManualResetEventSlim readEvent, ManualResetEventSlim continueEvent, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
            {
                try
                {
                    readerWriterLock.EnterReadLock();
                    readEvent.Set();
                    continueEvent.Wait(cancellationToken);
                }
                finally
                {
                    readerWriterLock.ExitReadLock();
                }
            },
            cancellationToken);
    }

    private static Task WriteAsync(ReaderWriterLockSlim readerWriterLock, ManualResetEventSlim writeEvent, ManualResetEventSlim continueEvent, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
            {
                try
                {
                    readerWriterLock.EnterWriteLock();
                    writeEvent.Set();
                    continueEvent.Wait(cancellationToken);
                }
                finally
                {
                    readerWriterLock.ExitWriteLock();
                }
            },
            cancellationToken);
    }
}
