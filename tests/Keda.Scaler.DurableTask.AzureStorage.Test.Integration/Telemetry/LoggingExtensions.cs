// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Text;
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
            => new UnitTestingLogger(categoryName, _context);

        public void Dispose()
            => GC.SuppressFinalize(this);
    }

    private sealed class UnitTestingLogger(string categoryName, TestContext context) : ILogger
    {
        private static readonly CompositeFormat MessageFormat = CompositeFormat.Parse("{0:O} {1} {2} {3}");

        private readonly string _categoryName = categoryName ?? throw new ArgumentNullException(nameof(categoryName));
        private readonly TestContext _context = context ?? throw new ArgumentNullException(nameof(context));

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel)
            => logLevel is not LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            ArgumentNullException.ThrowIfNull(formatter);

            string msg = formatter(state, exception);
            string log = string.Format(CultureInfo.InvariantCulture, MessageFormat, DateTimeOffset.UtcNow, _categoryName, logLevel.ToString().ToUpperInvariant(), msg);
            _context.WriteLine(log);
        }
    }
}
