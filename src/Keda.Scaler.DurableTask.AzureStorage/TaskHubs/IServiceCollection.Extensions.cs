// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHubs;

internal static class IServiceCollectionExtensions
{
    public static IServiceCollection AddDurableTaskScaleManager(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services
            .AddScoped<IConfigureOptions<TaskHubOptions>, ConfigureTaskHubOptions>()
            .AddScoped<BlobPartitionManager>()
            .AddScoped<TablePartitionManager>()
            .AddScoped<ITaskHubPartitionManager>(x =>
            {
                IOptionsSnapshot<TaskHubOptions> options = x.GetRequiredService<IOptionsSnapshot<TaskHubOptions>>();
                return options.Get(default).UseTablePartitionManagement
                    ? x.GetRequiredService<TablePartitionManager>()
                    : x.GetRequiredService<BlobPartitionManager>();
            })
            .AddScoped<ITaskHub, TaskHub>()
            .AddScoped<DurableTaskScaleManager, OptimalDurableTaskScaleManager>();
    }
}
