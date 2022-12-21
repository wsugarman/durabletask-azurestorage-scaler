// Copyright © William Sugarman.
// Licensed under the MIT License.

namespace Keda.Scaler.DurableTask.AzureStorage.Blobs;

internal static class LeasesContainer
{
    public const string TaskHubBlobName = "taskhub.json";

    public static string GetName(string? taskHub)
        => taskHub + "-leases";
}
