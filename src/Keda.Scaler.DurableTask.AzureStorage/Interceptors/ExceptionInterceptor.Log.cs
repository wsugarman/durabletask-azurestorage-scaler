// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Logging;

namespace Keda.Scaler.DurableTask.AzureStorage.Interceptors;

internal static partial class Log
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Critical,
        Message = "Caught unhandled exception!")]
    public static partial void CaughtUnhandledException(this ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "Request contains invalid input.")]
    public static partial void ReceivedInvalidInput(this ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "RPC operation canceled.")]
    public static partial void DetectedRequestCancellation(this ILogger logger, OperationCanceledException exception);
}
