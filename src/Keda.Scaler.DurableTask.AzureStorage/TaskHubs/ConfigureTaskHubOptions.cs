// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.Metadata;
using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHubs;

internal sealed class ConfigureTaskHubOptions(IOptionsSnapshot<ScalerOptions> scalerOptions) : IConfigureOptions<TaskHubOptions>
{
    private readonly ScalerOptions _scalerOptions = scalerOptions?.Get(default) ?? throw new ArgumentNullException(nameof(scalerOptions));

    public void Configure(TaskHubOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.MaxActivitiesPerWorker = _scalerOptions.MaxActivitiesPerWorker;
        options.MaxOrchestrationsPerWorker = _scalerOptions.MaxOrchestrationsPerWorker;
        options.TaskHubName = _scalerOptions.TaskHubName;
        options.UseTablePartitionManagement = _scalerOptions.UseTablePartitionManagement;
    }
}
