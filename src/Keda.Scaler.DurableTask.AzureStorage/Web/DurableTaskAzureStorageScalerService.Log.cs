// Copyright © William Sugarman.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace Keda.Scaler.DurableTask.AzureStorage;

internal static partial class Log
{
    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Information,
        Message = "Metric value for Task Hub '{TaskHubName}' is {MetricValue}.")]
    public static partial void ComputedScalerMetricValue(this ILogger logger, string taskHubName, long metricValue);

    [LoggerMessage(
        EventId = 10,
        Level = LogLevel.Information,
        Message = "Metric target for Task Hub '{TaskHubName}' is {MetricTarget}.")]
    public static partial void ComputedScalerMetricTarget(this ILogger logger, string taskHubName, long metricTarget);

    [LoggerMessage(
        EventId = 11,
        Level = LogLevel.Information,
        Message = "Task Hub '{TaskHubName}' is currently active.")]
    public static partial void DetectedActiveTaskHub(this ILogger logger, string taskHubName);

    [LoggerMessage(
        EventId = 12,
        Level = LogLevel.Information,
        Message = "Task Hub '{TaskHubName}' is not currently active.")]
    public static partial void DetectedInactiveTaskHub(this ILogger logger, string taskHubName);
}
