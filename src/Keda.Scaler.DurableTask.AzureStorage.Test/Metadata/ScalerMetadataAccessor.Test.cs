// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Keda.Scaler.DurableTask.AzureStorage.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Metadata;

[TestClass]
public class ScalerMetadataAccessorTest
{
    private readonly TestContext _testContext;

    public ScalerMetadataAccessorTest(TestContext testContext)
    {
        ArgumentNullException.ThrowIfNull(testContext);
        _testContext = testContext;
    }

    [TestMethod]
    public void ScalerMetadata_NonNull_AssignsValue()
    {
        ScalerMetadataAccessor accessor = new();
        Dictionary<string, string?> metadata = [];

        accessor.ScalerMetadata = metadata;
        Assert.AreSame(metadata, accessor.ScalerMetadata);
    }

    [TestMethod]
    public void ScalerMetadata_Null_ClearsValue()
    {
        ScalerMetadataAccessor accessor = new();
        Dictionary<string, string?> metadata = [];

        accessor.ScalerMetadata = metadata;
        Assert.AreSame(metadata, accessor.ScalerMetadata);

        accessor.ScalerMetadata = null;
        Assert.IsNull(accessor.ScalerMetadata);
    }

    [TestMethod]
    [Timeout(10 * 1000, CooperativeCancellation = true)]
    public async ValueTask ScalerMetadata_ValuePerTask_KeepsValuesLocal()
    {
        const int Max = 3;

        int assigned = 0;
        using ManualResetEventSlim resetEvent = new(false);
        ScalerMetadataAccessor accessor = new();

        Task[] tasks = [.. Enumerable
            .Repeat<object?>(null, 3)
            .Select(_ => Task.Run(() =>
            {
                Dictionary<string, string?> metadata = [];
                accessor.ScalerMetadata = metadata;

                if (Interlocked.Increment(ref assigned) is Max)
                    resetEvent.Set();

                resetEvent.Wait(_testContext.CancellationToken);
                Assert.AreSame(metadata, accessor.ScalerMetadata);
            }, _testContext.CancellationToken))];

        await Task.WhenAll(tasks);
    }
}
