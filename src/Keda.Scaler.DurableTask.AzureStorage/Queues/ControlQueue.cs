// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Globalization;

namespace Keda.Scaler.DurableTask.AzureStorage.Queues;

internal static class ControlQueue
{
    public static string GetName(string? taskHub, int partition)
    {
        if (partition < 0 || partition > 15)
            throw new ArgumentOutOfRangeException(nameof(partition));

        return string.Format(CultureInfo.InvariantCulture, "{0}-control-{1:D2}", taskHub, partition);
    }
}
