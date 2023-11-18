// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Keda.Scaler.DurableTask.AzureStorage.Queues;

internal static class ControlQueue
{
    // See: https://learn.microsoft.com/en-us/rest/api/storageservices/naming-queues-and-metadata#queue-names
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Queue names must be lowercase.")]
    public static string GetName(string? taskHub, int partition)
    {
        return partition is < 0 or > 15
            ? throw new ArgumentOutOfRangeException(nameof(partition))
            : string.Format(CultureInfo.InvariantCulture, "{0}-control-{1:D2}", taskHub?.ToLowerInvariant(), partition);
    }
}
