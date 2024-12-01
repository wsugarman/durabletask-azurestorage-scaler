// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Azure.Data.Tables;
using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHub;

internal sealed class TablePartitionManager(TableServiceClient tableServiceClient, IOptionsSnapshot<TaskHubOptions> options) : ITaskHubPartitionManager
{
    private readonly TableServiceClient _tableServiceClient = tableServiceClient ?? throw new ArgumentNullException(nameof(tableServiceClient));
    private readonly TaskHubOptions _options = options?.Get(default) ?? throw new ArgumentNullException(nameof(options));

    public IAsyncEnumerable<string> GetPartitionsAsync(CancellationToken cancellationToken = default)
    {
        // Query the table for partitions
        TableClient client = _tableServiceClient.GetTableClient(PartitionsTable.GetName(_options.TaskHubName));
        return client
            .QueryAsync<TableEntity>(select: [nameof(TableEntity.RowKey)], cancellationToken: cancellationToken)
            .Select(x => x.RowKey);
    }
}
