// Copyright © William Sugarman.
// Licensed under the MIT License.

namespace Keda.Scaler.DurableTask.AzureStorage.Queues;

internal static class WorkItemQueue
{
    public static string GetName(string? taskHub)
        => taskHub + "-workitems";
}
