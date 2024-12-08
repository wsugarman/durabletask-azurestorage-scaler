// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHubs;

internal sealed class ConfigureTaskHubOptions(IScalerMetadataAccessor accessor) : IConfigureOptions<TaskHubOptions>
{
    private readonly IScalerMetadataAccessor _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));

    public void Configure(TaskHubOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ScalerMetadata metadata = _accessor.ScalerMetadata ?? throw new InvalidOperationException(SR.ScalerMetadataNotFound);

        options.MaxActivitiesPerWorker = metadata.MaxActivitiesPerWorker;
        options.MaxOrchestrationsPerWorker = metadata.MaxOrchestrationsPerWorker;
        options.TaskHubName = metadata.TaskHubName;
        options.UseTablePartitionManagement = metadata.UseTablePartitionManagement;
    }
}
