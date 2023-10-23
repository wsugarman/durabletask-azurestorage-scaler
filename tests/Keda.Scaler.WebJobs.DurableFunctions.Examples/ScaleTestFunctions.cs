// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
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
        logger.LogInformation("Starting {Count} activities with duration '{Timeout}'.", input.ActivityCount, input.ActivityTime);

        return Task.WhenAll(Enumerable
            .Repeat((Context: context, Delay: input.ActivityTime), input.ActivityCount)
            .Select(x => x.Context.CallActivityAsync(nameof(WaitAsync), x.Delay)));
    }

    /// <inheritdoc cref="Task.Delay(TimeSpan)"/>
    [FunctionName(nameof(WaitAsync))]
    public static Task WaitAsync([ActivityTrigger] TimeSpan delay)
        => Task.Delay(delay);

    /// <summary>
    /// Asynchronously checks the health of the function app.
    /// </summary>
    /// <param name="req">An HTTP GET request.</param>
    /// <param name="client">A durable client.</param>
    /// <returns>
    /// A task representing the asynchronous orchestration. The value of its <see cref="Task{TResult}.Result"/>
    /// property is an <see cref="OkResult"/> if successful.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="req"/> or <paramref name="client"/> is <see langword="null"/>.
    /// </exception>
    [FunctionName(nameof(IsHealthyAsync))]
    public static async Task<IActionResult> IsHealthyAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "healthz")] HttpRequest req,
        [DurableClient] IDurableOrchestrationClient client)
    {
        if (req is null)
            throw new ArgumentNullException(nameof(req));

        if (client is null)
            throw new ArgumentNullException(nameof(client));

        // Run a query for all running instances to ensure the connection to the TaskHub is working correctly
        OrchestrationStatusQueryCondition conditions = new()
        {
            CreatedTimeFrom = DateTime.MinValue,
            CreatedTimeTo = DateTime.MaxValue,
            PageSize = 1,
            RuntimeStatus = new OrchestrationRuntimeStatus[]
            {
                OrchestrationRuntimeStatus.Pending,
                OrchestrationRuntimeStatus.Running,
            },
            ShowInput = false,
        };

        _ = await client.ListInstancesAsync(conditions, req.HttpContext.RequestAborted).ConfigureAwait(false);
        return new OkResult();
    }
}
