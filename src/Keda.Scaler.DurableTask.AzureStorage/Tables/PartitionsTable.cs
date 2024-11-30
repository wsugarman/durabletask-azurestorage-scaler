// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;

namespace Keda.Scaler.DurableTask.AzureStorage.Tables;

internal static class PartitionsTable
{
    // See: https://learn.microsoft.com/en-us/rest/api/storageservices/understanding-the-table-service-data-model#table-names
    public static string GetName(string taskHub)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskHub);
        return taskHub + "Partitions";
    }
}
