// Copyright © William Sugarman.
// Licensed under the MIT License.

namespace Keda.Scaler.DurableTask.AzureStorage.Cloud;

/// <summary>
/// Represents an Azure cloud environment.
/// </summary>
public enum CloudEnvironment
{
    // TODO: Add 'Private' as well

    /// <summary>
    /// Specifies an unknown Azure cloud.
    /// </summary>
    Unknown,

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

    /// <summary>
    /// Specifies the German Azure cloud.
    /// </summary>
    AzureGermanCloud,
}
