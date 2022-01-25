// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Mocks;

internal class MockCloudQueueClient : CloudQueueClient
{
    public MockCloudQueueClient() : base(new Uri("https://localhost"), default(StorageCredentials))
    {
    }
}
