// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHubs;

internal sealed partial class TablePartitionManager(TableServiceClient tableServiceClient, IOptionsSnapshot<TaskHubOptions> options, ILoggerFactory loggerFactory) : ITaskHubPartitionManager
{
    private readonly TableServiceClient _tableServiceClient = tableServiceClient ?? throw new ArgumentNullException(nameof(tableServiceClient));
    private readonly TaskHubOptions _options = options?.Get(default) ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger _logger = loggerFactory?.CreateLogger(LogCategories.Default) ?? throw new ArgumentNullException(nameof(loggerFactory));

    public async ValueTask<IReadOnlyList<string>> GetPartitionsAsync(CancellationToken cancellationToken = default)
    {
        // Query the table for partitions
        TableClient client = _tableServiceClient.GetTableClient(PartitionsTable.GetName(_options.TaskHubName));

        List<string> partitions;
        try
        {
            partitions = await client
                .QueryAsync<TableEntity>(select: [nameof(TableEntity.RowKey)], cancellationToken: cancellationToken)
                .Select(x => x.RowKey)
                .ToListAsync(cancellationToken);
        }
        catch (RequestFailedException rfe) when (rfe.Status is (int)HttpStatusCode.NotFound)
        {
            partitions = [];
        }

        if (partitions.Count > 0)
            LogTaskHubPartitionsTable(_logger, _options.TaskHubName, partitions.Count, client.Name);
        else
            LogMissingTaskHubPartitionsTable(_logger, _options.TaskHubName, client.Name);

        return partitions;
    }

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Found Task Hub '{TaskHubName}' with {Partitions} partitions in table '{TaskHubTableName}'.")]
    private static partial void LogTaskHubPartitionsTable(ILogger logger, string taskHubName, int partitions, string taskHubTableName);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Cannot find Task Hub '{TaskHubName}' partitions table blob '{TaskHubTableName}'.")]
    private static partial void LogMissingTaskHubPartitionsTable(ILogger logger, string taskHubName, string taskHubTableName);
}
