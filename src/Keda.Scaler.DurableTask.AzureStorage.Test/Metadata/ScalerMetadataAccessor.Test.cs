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
    private readonly ScalerMetadataAccessor _accessor;

    public ScalerMetadataAccessorTest(TestContext testContext)
    {
        ArgumentNullException.ThrowIfNull(testContext);
        _testContext = testContext;
        _accessor = new ScalerMetadataAccessor();
    }

    [TestMethod]
    public void ScalerMetadata_Default_ReturnsNull()
        => Assert.IsNull(_accessor.ScalerMetadata);

    [TestMethod]
    public void ScalerMetadata_NonNull_AssignsValue()
    {
        Dictionary<string, string?> metadata = [];

        _accessor.ScalerMetadata = metadata;
        Assert.AreSame(metadata, _accessor.ScalerMetadata);
    }

    [TestMethod]
    public void ScalerMetadata_Null_ClearsValue()
    {
        Dictionary<string, string?> metadata = [];

        _accessor.ScalerMetadata = metadata;
        Assert.AreSame(metadata, _accessor.ScalerMetadata);

        _accessor.ScalerMetadata = null;
        Assert.IsNull(_accessor.ScalerMetadata);
    }

    [TestMethod]
    [Timeout(10 * 1000, CooperativeCancellation = true)]
    public async ValueTask ScalerMetadata_ValuePerTask_KeepsValuesLocal()
    {
        const int Max = 3;

        int assigned = 0;
        using ManualResetEventSlim resetEvent = new(false);

        Task[] tasks = [.. Enumerable
            .Repeat<object?>(null, 3)
            .Select(_ => Task.Run(() =>
            {
                Dictionary<string, string?> metadata = [];
                _accessor.ScalerMetadata = metadata;

                if (Interlocked.Increment(ref assigned) is Max)
                    resetEvent.Set();

                resetEvent.Wait(_testContext.CancellationToken);
                Assert.AreSame(metadata, _accessor.ScalerMetadata);
            }, _testContext.CancellationToken))];

        await Task.WhenAll(tasks);
    }
}
