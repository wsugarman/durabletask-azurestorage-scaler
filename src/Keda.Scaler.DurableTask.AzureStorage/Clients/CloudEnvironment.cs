// Copyright © William Sugarman.
// Licensed under the MIT License.

namespace Keda.Scaler.DurableTask.AzureStorage.Clients;

/// <summary>
/// Represents an Azure cloud environment.
/// </summary>
public enum CloudEnvironment
{
    /// <summary>
    /// Specifies an unknown Azure cloud.
    /// </summary>
    Unknown,

    /// <summary>
    /// Specifies Azure Stack Hub or an air-gapped cloud.
    /// </summary>
    Private,

    /// <summary>
    /// Specifies the public Azure cloud.
    /// </summary>
    AzurePublicCloud,

    /// <summary>
    /// Specifies the US government Azure cloud.
    /// </summary>
    AzureUSGovernmentCloud,

    /// <summary>
    /// Specifies the Chinese Azure cloud.
    /// </summary>
    AzureChinaCloud,
}
