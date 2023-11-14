// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Integration.Logging;

internal sealed class TestContextLogger : ILogger
{
    private readonly string _categoryName;
    private readonly TestContext _testContext;

    public TestContextLogger(string categoryName, TestContext testContext)
    {
        _categoryName = categoryName ?? throw new ArgumentNullException(nameof(categoryName));
        _testContext = testContext ?? throw new ArgumentNullException(nameof(testContext));
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel)
        => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        if (formatter is null)
            throw new ArgumentNullException(nameof(formatter));

        string message = formatter(state, exception);
        if (string.IsNullOrEmpty(message))
            return;

        message = $"[{DateTime.UtcNow:O}] [{logLevel}] [{_categoryName}] [{eventId}]: {message}";

        if (exception != null)
            message += Environment.NewLine + Environment.NewLine + exception;

        _testContext.WriteLine(message);
    }
}
