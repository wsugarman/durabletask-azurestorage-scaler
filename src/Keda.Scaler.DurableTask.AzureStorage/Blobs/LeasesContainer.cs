// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Keda.Scaler.DurableTask.AzureStorage.Blobs;

internal static class LeasesContainer
{
    public const string TaskHubBlobName = "taskhub.json";

    // See: https://learn.microsoft.com/en-us/rest/api/storageservices/naming-and-referencing-containers--blobs--and-metadata#container-names
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Blob container names must be lowercase.")]
    public static string GetName(string taskHub)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskHub);
        return taskHub.ToLowerInvariant() + "-leases";
    }
}
