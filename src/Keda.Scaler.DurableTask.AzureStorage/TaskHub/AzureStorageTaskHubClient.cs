// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Queues;
using Keda.Scaler.DurableTask.AzureStorage.Accounts;
using Keda.Scaler.DurableTask.AzureStorage.Clients;
using Microsoft.Extensions.Logging;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHub;

/// <summary>
/// Represents a client for interacting with Durable Task Hubs that use the Azure Storage backend provider.
/// </summary>
public abstract class AzureStorageTaskHubClient
{
    private readonly IStorageAccountClientFactory<QueueServiceClient> _queueServiceClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureStorageTaskHubClient"/> class.
    /// </summary>
    /// <param name="queueServiceClientFactory">A factory for creating Azure Queue service clients.</param>
    /// <param name="loggerFactory">A factory for creating loggers.</param>
    /// <exception cref="ArgumentNullException"><paramref name="loggerFactory"/> is <see langword="null"/>.</exception>
    [SuppressMessage("Style", "IDE0290:Use primary constructor", Justification = "Preserve XML doc for class and ctor.")]
    protected AzureStorageTaskHubClient(
        IStorageAccountClientFactory<QueueServiceClient> queueServiceClientFactory,
        ILoggerFactory loggerFactory)
    {
        _queueServiceClientFactory = queueServiceClientFactory ?? throw new ArgumentNullException(nameof(queueServiceClientFactory));
        Logger = loggerFactory?.CreateLogger(LogCategories.Default) ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <summary>
    /// Gets a diagnostic logger for writing telemetry.
    /// </summary>
    /// <value>The <see cref="ILogger"/> object.</value>
    protected ILogger Logger { get; }

    /// <summary>
    /// Asynchronously attempts to retrieve an <see cref="ITaskHubQueueMonitor"/> for the Task Hub with the given name.
    /// </summary>
    /// <param name="accountInfo">The account information for the Azure Storage account.</param>
    /// <param name="taskHubName">The name of the desired Task Hub.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A value task that represents the asynchronous operation. The value of the type parameter
    /// of the value task contains the monitor for the given Task Hub or <c>0</c> if the Task Hub is not yet ready.
    /// </returns>
    /// <exception cref="ArgumentException"><paramref name="accountInfo"/> is missing information.</exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="accountInfo"/> or <paramref name="taskHubName"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> is canceled.</exception>
    /// <exception cref="RequestFailedException">
    /// A problem occurred connecting to the Storage Account based on <paramref name="accountInfo"/>.
    /// </exception>
    public virtual async ValueTask<ITaskHubQueueMonitor> GetMonitorAsync(AzureStorageAccountOptions accountInfo, string taskHubName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountInfo);
        ArgumentException.ThrowIfNullOrWhiteSpace(taskHubName);

        try
        {
            int partitionCount = await GetPartitionCountAsync(accountInfo, taskHubName, cancellationToken).ConfigureAwait(false);
            if (partitionCount is 0)
                return NullTaskHubQueueMonitor.Instance;

            return new TaskHubQueueMonitor(taskHubName, partitionCount, _queueServiceClientFactory.GetServiceClient(accountInfo), Logger);
        }
        catch (RequestFailedException rfe) when (rfe.Status is (int)HttpStatusCode.NotFound)
        {
            return NullTaskHubQueueMonitor.Instance;
        }
    }

    /// <summary>
    /// Asynchronously attempts to retrieve the number of partitions used for the Task Hub with the given name.
    /// </summary>
    /// <param name="accountInfo">The account information for the Azure Storage account.</param>
    /// <param name="taskHub">The name of the desired Task Hub.</param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A value task that represents the asynchronous operation. The value of the type parameter
    /// of the value task contains the partition count.
    /// </returns>
    /// <exception cref="ArgumentException"><paramref name="accountInfo"/> is missing information.</exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="accountInfo"/> or <paramref name="taskHub"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> is canceled.</exception>
    /// <exception cref="RequestFailedException">
    /// A problem occurred connecting to the Storage Account based on <paramref name="accountInfo"/>.
    /// </exception>
    protected abstract ValueTask<int> GetPartitionCountAsync(AzureStorageAccountOptions accountInfo, string taskHub, CancellationToken cancellationToken = default);
}
