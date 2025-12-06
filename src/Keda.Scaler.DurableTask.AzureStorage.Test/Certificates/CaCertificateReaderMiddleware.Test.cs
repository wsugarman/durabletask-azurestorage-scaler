// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Keda.Scaler.DurableTask.AzureStorage.Certificates;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Certificates;

[TestClass]
public class CaCertificateReaderMiddlewareTests
{
    private readonly TestContext _testContext;

    public CaCertificateReaderMiddlewareTests(TestContext testContext)
    {
        ArgumentNullException.ThrowIfNull(testContext);
        _testContext = testContext;
    }

    [TestMethod]
    public void GivenNullRequestDelegate_WhenCreatingMiddleware_ThenThrowArgumentNullException()
    {
        using ReaderWriterLockSlim readerWriterLock = new();
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new CaCertificateReaderMiddleware(null!, readerWriterLock));
    }

    [TestMethod]
    public void GivenNullReaderWriterLock_WhenCreatingMiddleware_ThenThrowArgumentNullException()
        => Assert.ThrowsExactly<ArgumentNullException>(() => new CaCertificateReaderMiddleware(NextAsync, null!));

    [TestMethod]
    public async ValueTask GivenNoOtherReaders_WhenInvokingMiddleware_ThenEnterReadLock()
    {
        using ReaderWriterLockSlim readerWriterLock = new();

        DefaultHttpContext context = new();
        CaCertificateReaderMiddleware middleware = new(NextAsync, readerWriterLock);

        await middleware.InvokeAsync(context);
        Assert.AreEqual(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [TestMethod]
    public async ValueTask GivenOtherReader_WhenInvokingMiddleware_ThenEnterReadLock()
    {
        using ReaderWriterLockSlim readerWriterLock = new();
        using ManualResetEventSlim readEvent = new(initialState: false);
        using ManualResetEventSlim middlewareEvent = new(initialState: false);

        DefaultHttpContext context = new() { RequestAborted = _testContext.CancellationToken };
        CaCertificateReaderMiddleware middleware = new(NextAsync, readerWriterLock);

        // Start the reader
        Task readerTask = ReadAsync(readerWriterLock, readEvent, middlewareEvent, _testContext.CancellationToken);
        readEvent.Wait(_testContext.CancellationToken);

        await middleware.InvokeAsync(context);

        middlewareEvent.Set();
        await readerTask;
        Assert.AreEqual(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [TestMethod]
    public async ValueTask GivenWriter_WhenInvokingMiddleware_ThenWaitForWriter()
    {
        using ReaderWriterLockSlim readerWriterLock = new();
        using ManualResetEventSlim writeEvent = new(initialState: false);
        using ManualResetEventSlim middlewareEvent = new(initialState: false);

        DefaultHttpContext context = new() { RequestAborted = _testContext.CancellationToken };
        CaCertificateReaderMiddleware middleware = new(NextAsync, readerWriterLock);

        // Start the writer
        Task writerTask = WriteAsync(readerWriterLock, writeEvent, middlewareEvent, _testContext.CancellationToken);
        writeEvent.Wait(_testContext.CancellationToken);

        // Wait for the middleware to wait on its read
        Task httpTask = Task.Run(() => middleware.InvokeAsync(context), _testContext.CancellationToken);
        while (readerWriterLock.WaitingReadCount is 0)
            await Task.Delay(100, _testContext.CancellationToken);

        middlewareEvent.Set();
        await Task.WhenAll(writerTask, httpTask);
        Assert.AreEqual(StatusCodes.Status200OK, context.Response.StatusCode);
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
