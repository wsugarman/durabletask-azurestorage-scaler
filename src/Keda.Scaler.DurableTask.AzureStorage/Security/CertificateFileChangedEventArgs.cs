// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.IO;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

internal class CertificateFileChangedEventArgs : EventArgs
{
    public WatcherChangeTypes ChangeType { get; init; }
}
