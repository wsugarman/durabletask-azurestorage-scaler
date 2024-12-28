// Copyright © William Sugarman.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace Keda.Scaler.DurableTask.AzureStorage.Certificates;

internal static partial class Log
{
    [LoggerMessage(
        EventId = 13,
        Level = LogLevel.Information,
        Message = "The custom CA certificate at '{Path}' has been reloaded with thumbprint {Thumbprint}.")]
    public static partial void ReloadedCustomCertificateAuthority(this ILogger logger, string path, string thumbprint);
}
