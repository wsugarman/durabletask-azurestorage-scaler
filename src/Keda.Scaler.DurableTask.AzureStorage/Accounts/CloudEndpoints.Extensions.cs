// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;

namespace Keda.Scaler.DurableTask.AzureStorage.Accounts;

internal static class CloudEndpointsExtensions
{
    public static Uri GetStorageServiceUri(this CloudEndpoints endpoints, string account, AzureStorageService service)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
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
                endpoints.StorageSuffix),
            UriKind.Absolute);
#pragma warning restore CA1308
    }
}
