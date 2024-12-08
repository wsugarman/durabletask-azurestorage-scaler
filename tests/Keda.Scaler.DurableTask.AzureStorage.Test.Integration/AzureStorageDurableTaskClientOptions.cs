// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using DurableTask.AzureStorage;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Integration;

[SuppressMessage("Microsoft.Performance", "CA1812:Avoid uninstantiated internal classes.", Justification = "This class is instantiated via dependency injection.")]
internal sealed class AzureStorageDurableTaskClientOptions
{
    public const string DefaultSectionName = "DurableTask";

    [Required]
    public string ConnectionString { get; set; } = "UseDevelopmentStorage=true";

    [Range(1, 16)]
    public int PartitionCount { get; set; } = 4;

    [Required]
    public string TaskHubName { get; set; } = "TestHubName";

    public AzureStorageOrchestrationServiceSettings ToOrchestrationServiceSettings()
    {
        return new()
        {
            PartitionCount = PartitionCount,
            TaskHubName = TaskHubName,
            StorageAccountClientProvider = new StorageAccountClientProvider(ConnectionString)
        };
    }
}
