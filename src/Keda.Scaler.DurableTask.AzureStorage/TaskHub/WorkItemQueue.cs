// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHub;

internal static class WorkItemQueue
{
    // See: https://learn.microsoft.com/en-us/rest/api/storageservices/naming-queues-and-metadata#queue-names
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Queue names must be lowercase.")]
    public static string GetName(string taskHub)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskHub);
        return taskHub.ToLowerInvariant() + "-workitems";
    }
}
