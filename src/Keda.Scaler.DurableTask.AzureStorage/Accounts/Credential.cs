// Copyright © William Sugarman.
// Licensed under the MIT License.

namespace Keda.Scaler.DurableTask.AzureStorage.Accounts;

/// <summary>
/// Represents the possible credentials used by Azure Storage connections.
/// </summary>
public static class Credential
{
    /// <summary>
    /// The credential for using managed identity.
    /// </summary>
    public const string ManagedIdentity = nameof(ManagedIdentity);
}
