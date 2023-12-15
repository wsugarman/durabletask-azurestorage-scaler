// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;

namespace Keda.Scaler.DurableTask.AzureStorage.Accounts;

/// <summary>
/// Represents the possible credentials used by Azure Storage connections.
/// </summary>
public static class Credentials
{
    /// <summary>
    /// The credential for using managed identity.
    /// </summary>
    [Obsolete("Use WorkloadIdentity instead.")]
    public const string ManagedIdentity = nameof(ManagedIdentity);

    /// <summary>
    /// The credential for using workload identity.
    /// </summary>
    public const string WorkloadIdentity = nameof(WorkloadIdentity);
}
