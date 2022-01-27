// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Keda.Scaler.DurableTask.AzureStorage.Monitor;

/// <summary>
/// Settings for <see cref="PerformanceMonitor"/>
/// </summary>
[ExcludeFromCodeCoverage]
internal class PerformanceMonitorOptions
{
    /// <summary>
    /// Gets or sets task hub name.
    /// </summary>
    public string TaskHubName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets partition count.
    /// </summary>
    public int PartitionCount { get; set; }
}
