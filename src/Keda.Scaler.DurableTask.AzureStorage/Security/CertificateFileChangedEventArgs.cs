// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography.X509Certificates;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

internal class CertificateFileChangedEventArgs : EventArgs
{
    public X509Certificate2? Certificate { get; init; }

    public ExceptionDispatchInfo? Exception { get; init; }
}
