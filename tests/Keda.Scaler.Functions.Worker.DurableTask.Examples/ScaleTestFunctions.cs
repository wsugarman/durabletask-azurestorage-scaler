// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace Keda.Scaler.Functions.Worker.DurableTask.Examples;

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
    /// <param name="input">The input for the scale test.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests from the host.
    /// The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>A task representing the asynchronous orchestration.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="context"/> or <paramref name="input"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">The task has been canceled.</exception>
    [Function(nameof(RunAsync))]
    public static Task RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context,
        ScaleTestInput input,
        CancellationToken cancellationToken = default)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        if (input is null)
            throw new ArgumentNullException(nameof(input));

        ILogger logger = context.CreateReplaySafeLogger("DurableTask.AzureStorage.Keda.Tests");
        logger.LogInformation("Starting {Count} activities with duration '{Timeout}'.", input.ActivityCount, input.ActivityTime);

        return Task.WhenAll(Enumerable
            .Repeat((Context: context, Delay: input.ActivityTime), input.ActivityCount)
            .Select(x => x.Context.CallActivityAsync(nameof(WaitAsync), x.Delay)));
    }

    /// <inheritdoc cref="Task.Delay(TimeSpan)"/>
    [Function(nameof(WaitAsync))]
    public static Task WaitAsync([ActivityTrigger] TimeSpan delay)
        => Task.Delay(delay);

    /// <summary>
    /// Asynchronously checks the health of the function app.
    /// </summary>
    /// <param name="request">An HTTP GET request.</param>
    /// <param name="client">A Durable Task client.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests from the host.
    /// The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous orchestration. The value of its <see cref="Task{TResult}.Result"/>
    /// property is an instance of the <see cref="HttpResponseData"/> class indicating success.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="request"/> or <paramref name="client"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">The task has been canceled.</exception>
    [Function(nameof(IsHealthyAsync))]
    public static async Task<HttpResponseData> IsHealthyAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "healthz")] HttpRequestData request,
        [DurableClient] DurableTaskClient client,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (client is null)
            throw new ArgumentNullException(nameof(client));

        // Run a query for all running instances to ensure the connection to the TaskHub is working correctly
        var query = new OrchestrationQuery
        {
            CreatedFrom = DateTimeOffset.MinValue,
            CreatedTo = DateTimeOffset.MaxValue,
            PageSize = 1,
            Statuses = new OrchestrationRuntimeStatus[]
            {
                OrchestrationRuntimeStatus.Pending,
                OrchestrationRuntimeStatus.Running,
            },
            FetchInputsAndOutputs = false,
        };

        await client
            .GetAllInstancesAsync(query)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return request.CreateResponse(HttpStatusCode.OK);
    }
}
