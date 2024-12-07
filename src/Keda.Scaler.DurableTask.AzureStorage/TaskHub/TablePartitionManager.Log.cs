// Copyright © William Sugarman.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHub;

internal static partial class Log
{
    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Found Task Hub '{TaskHubName}' with {Partitions} partitions in table '{TaskHubTableName}'.")]
    public static partial void FoundTaskHubPartitionsTable(this ILogger logger, string taskHubName, int partitions, string taskHubTableName);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Warning,
        Message = "Cannot find Task Hub '{TaskHubName}' partitions table blob '{TaskHubTableName}'.")]
    public static partial void CannotFindTaskHubPartitionsTable(this ILogger logger, string taskHubName, string taskHubTableName);
}
