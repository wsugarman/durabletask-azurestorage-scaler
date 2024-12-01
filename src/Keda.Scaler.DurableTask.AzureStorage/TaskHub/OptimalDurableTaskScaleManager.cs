// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHub;

internal sealed class OptimalDurableTaskScaleManager(ITaskHub taskHub, IOptionsSnapshot<TaskHubOptions> options, ILoggerFactory loggerFactory) : DurableTaskScaleManager(taskHub, options, loggerFactory)
{
    protected override int GetWorkerCount(TaskHubQueueUsage usage)
    {
        ArgumentNullException.ThrowIfNull(usage);

        WorkerSet workers = new(PartitionSet.FromWorkItems(usage.ControlQueueMessages));
        while (workers.RemainingPartitions.Count > 0)
        {
            PartitionSet workerPartitions = MaximizeWorkerPartitions(usage.ControlQueueMessages, workers.RemainingPartitions, Options.MaxOrchestrationsPerWorker);
            workers = workers.AddWorker(workerPartitions);
        }

        return workers.Count;
    }

    private static PartitionSet MaximizeWorkerPartitions(IReadOnlyList<int> partitionWorkItems, PartitionSet available, int maxOrchestrationWorkItems)
    {
#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
        PartitionSet[,] maxWorkerPartitions = new PartitionSet[partitionWorkItems.Count + 1, maxOrchestrationWorkItems + 1];
#pragma warning restore CA1814

        // Below is a dynamic programming solution for those nostalgic for their CS classes
        for (int p = 1; p <= partitionWorkItems.Count; p++)
        {
            for (int c = 1; c <= maxOrchestrationWorkItems; c++)
            {
                PartitionSet exclude = maxWorkerPartitions[p - 1, c];
                int workItems = Math.Min(partitionWorkItems[p - 1], maxOrchestrationWorkItems);

                // Skip this partition if:
                // (1) it was taken by another worker (partitions with 0 are also included here) OR
                // (2) there is not enough space left on the worker to process this partition's pending messages
                if (!available.Contains(p - 1) || workItems > c)
                {
                    maxWorkerPartitions[p, c] = exclude; // Same answer as if excluding the partition
                }
                else
                {
                    // Otherwise, choose the set of partitions that is larger
                    // Note: On a tie, we prefer that the partition is excluded to maximize the remaining capacity
                    PartitionSet include = maxWorkerPartitions[p - 1, c - workItems].Add(p - 1);
                    maxWorkerPartitions[p, c] = exclude.Count >= include.Count ? exclude : include;
                }
            }
        }

        return maxWorkerPartitions[partitionWorkItems.Count, maxOrchestrationWorkItems];
    }

    private readonly struct PartitionSet
    {
        private readonly ushort _partitionBitSet;
        private readonly byte _count;

        public int Count => _count;

        private PartitionSet(ushort partitions, byte count)
        {
            _count = count;
            _partitionBitSet = partitions;
        }

        // Precondition: Partition does not yet exist
        public PartitionSet Add(int partition)
            => new((ushort)(_partitionBitSet | (1 << partition)), (byte)(_count + 1));

        public bool Contains(int partition)
            => (_partitionBitSet & (1 << partition)) != 0;

        // Preconditions: There are no overlaps in bits and this.Count >= other.Count
        public PartitionSet Remove(PartitionSet other)
            => new((ushort)(_partitionBitSet & ~other._partitionBitSet), (byte)(_count - other._count));

        [ExcludeFromCodeCoverage(Justification = "Only used for debugging.")]
        public override string ToString()
            => _partitionBitSet.ToString("X4", CultureInfo.InvariantCulture);

        public static PartitionSet FromWorkItems(IReadOnlyList<int> partitionWorkItems)
        {
            PartitionSet result = default;
            for (int i = 0; i < partitionWorkItems.Count; i++)
            {
                if (partitionWorkItems[i] > 0)
                    result = result.Add(i);
            }

            return result;
        }
    }

    private readonly struct WorkerSet
    {
        public int Count => _count;

        public PartitionSet RemainingPartitions { get; }

        private readonly byte _count;

        public WorkerSet(PartitionSet start)
            : this(0, start)
        { }

        private WorkerSet(byte count, PartitionSet remaining)
        {
            _count = count;
            RemainingPartitions = remaining;
        }

        public WorkerSet AddWorker(PartitionSet partitions)
            => new((byte)(_count + 1), RemainingPartitions.Remove(partitions));
    }
}
