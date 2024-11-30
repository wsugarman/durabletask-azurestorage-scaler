// Copyright © William Sugarman.
// Licensed under the MIT License.

namespace Keda.Scaler.DurableTask.AzureStorage.Accounts;

/// <summary>
/// Represents the possible credentials used by Azure Storage connections.
/// </summary>
public static class Credentials
{
    /// <summary>
    /// The credential for using workload identity.
    /// </summary>
    public const string WorkloadIdentity = nameof(WorkloadIdentity);
}
