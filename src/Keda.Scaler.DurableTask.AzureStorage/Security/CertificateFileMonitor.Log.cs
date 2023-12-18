// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Logging;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

internal static partial class Log
{
    [LoggerMessage(
        EventId = 16,
        Level = LogLevel.Information,
        Message = "Successfully loaded certificate with thumbprint '{Thumbprint}' from '{Path}'.")]
    public static partial void LoadedCertificate(this ILogger logger, string thumbprint, string path);

    [LoggerMessage(
        EventId = 17,
        Level = LogLevel.Error,
        Message = "Unable to load certificate from '{Path}'.")]
    public static partial void FailedLoadingCertificate(this ILogger logger, Exception exception, string path);

    [LoggerMessage(
        EventId = 18,
        Level = LogLevel.Debug,
        Message = "Skipped reload event because the newly loaded certificate has the same thumbprint '{Thumbprint}'.")]
    public static partial void SkippedReloadEventForDuplicateThumbprint(this ILogger logger, string thumbprint);

    [LoggerMessage(
        EventId = 19,
        Level = LogLevel.Debug,
        Message = "Skipped reload event because the previous certificate file failed to load.")]
    public static partial void SkippedReloadEventForContinuedError(this ILogger logger);
}
