// Copyright © William Sugarman.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

internal static partial class Log
{
    [LoggerMessage(
        EventId = 13,
        Level = LogLevel.Information,
        Message = "Configured Kestrel server to use a certificate from '{Path}' and key from '{KeyPath}'.")]
    public static partial void ConfiguredServerCertificate(this ILogger logger, string path, string? keyPath);

    [LoggerMessage(
        EventId = 14,
        Level = LogLevel.Information,
        Message = "Configured Kestrel server to require a client certificate.")]
    public static partial void RequiredClientCertificate(this ILogger logger);

    [LoggerMessage(
        EventId = 15,
        Level = LogLevel.Information,
        Message = "Configured Kestrel server to validate client TLS certificates using CA from '{Path}'.")]
    public static partial void ConfiguredClientCertificateValidation(this ILogger logger, string path);
}
