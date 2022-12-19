// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHub;

internal sealed class OptimalOrchestrationAllocator : IOrchestrationAllocator
{
    public int GetWorkerCount(IReadOnlyList<long> partitionWorkItems, int maxOrchestrationWorkItems)
    {
        if (partitionWorkItems == null)
            throw new ArgumentNullException(nameof(partitionWorkItems));

        if (maxOrchestrationWorkItems < 1)
            throw new ArgumentOutOfRangeException(nameof(maxOrchestrationWorkItems));

        if (partitionWorkItems.Count == 0)
            return 0;

        WorkerSet workers = new WorkerSet();
        PartitionSet partitions = new PartitionSet(partitionWorkItems.Count);
        do
        {
            PartitionSet workerPartitions = MaximizeWorkerPartitions(partitionWorkItems, partitions, maxOrchestrationWorkItems);
            workers.AddWorker(workerPartitions);
        } while (workers.RemainingPartitions.Count > 0);

        return workers.Count;
    }

    private static PartitionSet MaximizeWorkerPartitions(IReadOnlyList<long> partitionWorkItems, PartitionSet available, int maxOrchestrationWorkItems)
    {
#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
        PartitionSet[,] maxWorkerPartitions = new PartitionSet[partitionWorkItems.Count + 1, maxOrchestrationWorkItems + 1];
#pragma warning restore CA1814

        // Below is a dynamic programming solution for those nostalgic for their CS classes
        for (int p = 0; p < partitionWorkItems.Count; p++)
        {
            for (int c = 0; c <= maxOrchestrationWorkItems; c++)
            {
                PartitionSet exclude = maxWorkerPartitions[p, c];
                int workItems = (int)Math.Min(partitionWorkItems[p], maxOrchestrationWorkItems);

                // Skip this partition if:
                // (1) it was taken by another worker OR
                // (2) there are no pending messages for the partition OR
                // (3) there is not enough space left on the worker to process this partition's pending messages
                if (!available.Contains(p) || workItems == 0 || workItems > c)
                {
                    maxWorkerPartitions[p + 1, c] = exclude; // Same answer as if excluding the partition
                }
                else
                {
                    // Otherwise, choose the set of partitions that is larger
                    // Note: On a tie, we prefer that the partition is excluded to maximize the remaining capacity
                    PartitionSet include = maxWorkerPartitions[p, c - workItems].Add(p);
                    maxWorkerPartitions[p + 1, c] = exclude.Count >= include.Count ? exclude : include;
                }
            }
        }

        return maxWorkerPartitions[partitionWorkItems.Count, maxOrchestrationWorkItems];
    }

    private readonly struct PartitionSet
    {
        public int Count => _count;

        private readonly ushort _partitionBitSet;
        private readonly byte _count;

        public PartitionSet(int count)
            : this((ushort)~(ushort.MaxValue & 2 << count - 1), (byte)count)
        { }

        private PartitionSet(ushort partitions, byte count)
        {
            _count = count;
            _partitionBitSet = partitions;
        }

        // Precondition: Partition does not yet exist
        public PartitionSet Add(int partition)
            => new PartitionSet((ushort)(_partitionBitSet | 2 << partition), (byte)(_count + 1));

        public bool Contains(int partition)
            => (_partitionBitSet & 2 << partition) != 0;

        // Preconditions: There are no overlaps in bits and this.Count >= other.Count
        public PartitionSet Remove(PartitionSet other)
            => new PartitionSet((ushort)(_partitionBitSet & ~other._partitionBitSet), (byte)(_count - other._count));
    }

    private readonly struct WorkerSet
    {
        public int Count => _count;

        public PartitionSet RemainingPartitions { get; }

        private readonly byte _count;

        private WorkerSet(byte count, PartitionSet remaining)
        {
            _count = count;
            RemainingPartitions = remaining;
        }

        public WorkerSet AddWorker(PartitionSet partitions)
            => new WorkerSet((byte)(_count + 1), RemainingPartitions.Remove(partitions));
    }
}
