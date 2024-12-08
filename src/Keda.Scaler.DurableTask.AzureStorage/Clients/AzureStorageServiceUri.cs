// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Globalization;
using System;

namespace Keda.Scaler.DurableTask.AzureStorage.Clients;

internal static class AzureStorageServiceUri
{
    public const string PublicSuffix = "core.windows.net";

    public const string USGovernmentSuffix = "core.usgovcloudapi.net";

    public const string ChinaSuffix = "core.chinacloudapi.cn";

    public static Uri Create(string accountName, AzureStorageService service, string endpointSuffix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountName);
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointSuffix);

        if (!Enum.IsDefined(service))
            throw new ArgumentOutOfRangeException(nameof(service));

#pragma warning disable CA1308 // Normalize strings to uppercase
        return new Uri(
            string.Format(
                CultureInfo.InvariantCulture,
                "https://{0}.{1}.{2}",
                accountName,
                service.ToString("G").ToLowerInvariant(),
                endpointSuffix),
            UriKind.Absolute);
#pragma warning restore CA1308
    }
}
