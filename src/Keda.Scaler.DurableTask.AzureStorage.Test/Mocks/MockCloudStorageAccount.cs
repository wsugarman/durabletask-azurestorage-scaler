// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Mocks;

internal class MockCloudStorageAccount : CloudStorageAccount
{
    public MockCloudStorageAccount() : base(new StorageCredentials("testAccount","testkeyvalue"), true)
    {
    }
}
