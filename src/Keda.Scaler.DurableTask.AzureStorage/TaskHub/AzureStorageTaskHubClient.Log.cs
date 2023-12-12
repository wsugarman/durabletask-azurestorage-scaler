// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Logging;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHub;

internal static partial class Log
{
    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Found Task Hub '{TaskHubName}' with {Partitions} partitions created at {CreatedTime:O}.")]
    public static partial void FoundTaskHub(this ILogger logger, string? taskHubName, int partitions, DateTimeOffset createdTime);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Warning,
        Message = "Cannot find Task Hub '{TaskHubName}' metadata blob '{TaskHubBlobName}' in container '{LeaseContainerName}'.")]
    public static partial void CouldNotFindTaskHub(this ILogger logger, string? taskHubName, string taskHubBlobName, string leaseContainerName);
}

