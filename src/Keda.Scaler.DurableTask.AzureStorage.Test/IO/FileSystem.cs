// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.IO;

internal static class FileSystem
{
    // See: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/file-providers?view=aspnetcore-8.0#watch-for-changes
    public const int PollingIntervalMs = 4000;

    public static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(4);
}
