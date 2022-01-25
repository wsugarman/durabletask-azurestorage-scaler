// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Mocks;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Mock CloudQueue")]
internal class MockCloudQueue : CloudQueue
{
    public MockCloudQueue() : base(new Uri("https://localhost"))
    {
    }
}
