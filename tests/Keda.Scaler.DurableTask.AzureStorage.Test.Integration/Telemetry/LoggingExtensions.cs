// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Integration.DependencyInjection;

internal static class LoggingExtensions
{
    public static ILoggingBuilder AddUnitTesting(this ILoggingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        ServiceDescriptor descriptor = ServiceDescriptor.Singleton<ILoggerProvider, UnitTestingLoggerProvider>(sp => new UnitTestingLoggerProvider(sp.GetRequiredService<TestContext>()));
        builder.Services.TryAddEnumerable(descriptor);
        return builder;
    }

    private sealed class UnitTestingLoggerProvider(TestContext context) : ILoggerProvider
    {
        private readonly TestContext _context = context ?? throw new ArgumentNullException(nameof(context));

        public ILogger CreateLogger(string categoryName)
            => new UnitTestingLogger(_context);

        public void Dispose()
            => GC.SuppressFinalize(this);
    }

    private sealed class UnitTestingLogger(TestContext context) : ILogger
    {
        private readonly TestContext _context = context ?? throw new ArgumentNullException(nameof(context));

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel)
            => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            ArgumentNullException.ThrowIfNull(formatter);

            MessageLevel msgLevel = logLevel switch
            {
                LogLevel.Debug => MessageLevel.Informational,
                LogLevel.Information => MessageLevel.Informational,
                LogLevel.Warning => MessageLevel.Warning,
                _ => MessageLevel.Error,
            };

            string msg = formatter(state, exception);
            _context.DisplayMessage(msgLevel, msg);
        }
    }
}
