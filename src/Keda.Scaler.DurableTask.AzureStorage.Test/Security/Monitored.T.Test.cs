// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Keda.Scaler.DurableTask.AzureStorage.Security;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Security;

[TestClass]
public class MonitoredTest
{
    [TestMethod]
    public void Reload()
    {
        int reloads = 0;
        Queue<CancellationTokenSource> sources = new Queue<CancellationTokenSource>();
        using Monitored<string> monitored = new Monitored<string>(
            () => reloads++.ToString(CultureInfo.InvariantCulture),
            () =>
            {
                CancellationTokenSource source = new CancellationTokenSource();
                sources.Enqueue(source);
                return new CancellationChangeToken(source.Token);
            });

        Assert.AreEqual(1, sources.Count);
        Assert.AreEqual(0, int.Parse(monitored.Current, CultureInfo.InvariantCulture));

        using CancellationTokenSource first = sources.Dequeue();
        first.Cancel();
        Assert.AreEqual(1, int.Parse(monitored.Current, CultureInfo.InvariantCulture));

        using CancellationTokenSource second = sources.Dequeue();
        second.Cancel();
        Assert.AreEqual(2, int.Parse(monitored.Current, CultureInfo.InvariantCulture));

        Assert.AreEqual(1, sources.Count);
        foreach (CancellationTokenSource source in sources)
            source.Dispose();
    }
}
