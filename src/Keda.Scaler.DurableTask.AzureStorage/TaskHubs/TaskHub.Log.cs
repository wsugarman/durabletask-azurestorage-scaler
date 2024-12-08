// Copyright © William Sugarman.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHubs;

internal static partial class Log
{
    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Warning,
        Message = "Could not find control queue '{ControlQueueName}'.")]
    public static partial void CouldNotFindControlQueue(this ILogger logger, string controlQueueName);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Warning,
        Message = "Could not find work item queue '{WorkItemQueueName}'.")]
    public static partial void CouldNotFindWorkItemQueue(this ILogger logger, string workItemQueueName);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Debug,
        Message = "Found {WorkItemCount} work item messages and the following control queue message counts [{ControlCounts}].")]
    public static partial void FoundTaskHubQueues(this ILogger logger, int workItemCount, string controlCounts);
}
