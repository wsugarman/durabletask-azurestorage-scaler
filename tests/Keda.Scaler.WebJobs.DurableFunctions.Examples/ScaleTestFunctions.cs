// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace Keda.Scaler.WebJobs.DurableFunctions.Examples;

/// <summary>
/// A collection of Durable Function orchstrations and activities meant to help test
/// the KEDA external scaler for the Durable Task framework.
/// </summary>
public static class ScaleTestFunctions
{
    /// <summary>
    /// Asynchronously triggrs the specified number of activities that run for a variable amount of time.
    /// </summary>
    /// <param name="context">The orchestration context.</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>A task representing the asynchronous orchestration.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="context"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(RunAsync))]
    public static Task RunAsync(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        ILogger logger)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        if (logger is null)
            throw new ArgumentNullException(nameof(logger));

        ScaleTestInput input = context.GetInput<ScaleTestInput>();

        logger = context.CreateReplaySafeLogger(logger);
        logger.LogInformation("Starting {Count} activities with duration '{Timeout}'", input.ActivityCount, input.ActivityTime);

        return Task.WhenAll(Enumerable
            .Repeat((Context: context, Delay: input.ActivityTime), input.ActivityCount)
            .Select(x => x.Context.CallActivityAsync(nameof(WaitAsync), x.Delay)));
    }

    /// <inheritdoc cref="Task.Delay(TimeSpan)"/>
    [FunctionName(nameof(WaitAsync))]
    public static Task WaitAsync([ActivityTrigger] TimeSpan delay)
        => Task.Delay(delay);
}
