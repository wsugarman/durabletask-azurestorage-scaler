// Copyright © William Sugarman.
// Licensed under the MIT License.

using Keda.Scaler.DurableTask.AzureStorage.Accounts;
using System.Globalization;
using System;

namespace Keda.Scaler.DurableTask.AzureStorage.Clouds;

internal static class AzureStorageEndpoint
{
    public const string PublicSuffix = "core.windows.net";

    public const string USGovernmentSuffix = "core.usgovcloudapi.net";

    public const string ChinaSuffix = "core.chinacloudapi.cn";

    public static Uri GetStorageServiceUri(string account, AzureStorageService service, string endpointSuffix)
    {
        ArgumentNullException.ThrowIfNull(endpointSuffix);
        ArgumentException.ThrowIfNullOrWhiteSpace(account);

        if (!Enum.IsDefined(service))
            throw new ArgumentOutOfRangeException(nameof(service));

#pragma warning disable CA1308 // Normalize strings to uppercase
        return new Uri(
            string.Format(
                CultureInfo.InvariantCulture,
                "https://{0}.{1}.{2}",
                account,
                service.ToString("G").ToLowerInvariant(),
                endpointSuffix),
            UriKind.Absolute);
#pragma warning restore CA1308
    }
}
