// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Logging;

internal static class XUnitLogger
{
    public static ILogger<T> Create<T>(ITestOutputHelper outputHelper)
    {
        ArgumentNullException.ThrowIfNull(outputHelper);

        return new ServiceCollection()
            .AddLogging(x => x.AddXUnit(outputHelper))
            .BuildServiceProvider()
            .GetRequiredService<ILogger<T>>();
    }

    public static ILoggerFactory CreateFactory(ITestOutputHelper outputHelper)
    {
        ArgumentNullException.ThrowIfNull(outputHelper);

        return new ServiceCollection()
            .AddLogging(x => x.AddXUnit(outputHelper))
            .BuildServiceProvider()
            .GetRequiredService<ILoggerFactory>();
    }
}
