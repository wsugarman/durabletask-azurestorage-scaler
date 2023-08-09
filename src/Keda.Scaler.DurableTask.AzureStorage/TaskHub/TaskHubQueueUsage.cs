// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHub;

/// <summary>
/// Represents the current activity in the Azure Storage queues used by a Durable Task Hub
/// with the Azure Storage backend provider.
/// </summary>
public sealed class TaskHubQueueUsage
{
    /// <summary>
    /// Gets the approximate number of messages per control queue partition.
    /// </summary>
    /// <remarks>
    /// The i-th element represents partition <c>i</c>.
    /// </remarks>
    /// <value>A list of the control queue message counts.</value>
    public IReadOnlyList<int> ControlQueueMessages { get; }

    /// <summary>
    /// Gets a value indicating whether there is currently any activity for the Task Hub.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if there is at least one message that is pending; otherwise, <see langword="false"/>.
    /// </value>
    public bool HasActivity => WorkItemQueueMessages > 0 || ControlQueueMessages.Any(x => x > 0);

    /// <summary>
    /// Gets the approximate number of messages in the work item queue.
    /// </summary>
    /// <value>The number of work item messages.</value>
    public int WorkItemQueueMessages { get; }

    /// <summary>
    /// Gets a <see cref="TaskHubQueueUsage"/> object that represents no activity.
    /// </summary>
    /// <value>An object representing no activity.</value>
    public static TaskHubQueueUsage None { get; } = new TaskHubQueueUsage(Array.Empty<int>(), 0);

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskHubQueueUsage"/> class.
    /// </summary>
    /// <param name="controlQueueMessages">The approximate number of messages per control queue partition.</param>
    /// <param name="workItemQueueMessages">The approximate number of messages in the work item queue.</param>
    /// <exception cref="ArgumentNullException"><paramref name="controlQueueMessages"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="workItemQueueMessages"/> is less than <c>0</c>.</exception>
    public TaskHubQueueUsage(IReadOnlyList<int> controlQueueMessages, int workItemQueueMessages)
    {
        ControlQueueMessages = controlQueueMessages ?? throw new ArgumentNullException(nameof(controlQueueMessages));
        WorkItemQueueMessages = workItemQueueMessages < 0
            ? throw new ArgumentOutOfRangeException(nameof(workItemQueueMessages))
            : workItemQueueMessages;
    }
}
