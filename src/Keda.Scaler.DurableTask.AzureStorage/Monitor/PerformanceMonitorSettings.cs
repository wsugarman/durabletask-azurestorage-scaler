// Copyright © William Sugarman.
// Licensed under the MIT License.

namespace Keda.Scaler.DurableTask.AzureStorage.Monitor;

internal class PerformanceMonitorSettings
{
    public string TaskHubName { get; set; } = string.Empty;

    public int PartitionCount { get; set; }
}
