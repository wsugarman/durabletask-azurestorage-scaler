// Copyright © William Sugarman.
// Licensed under the MIT License.

namespace Keda.Scaler.DurableTask.AzureStorage.Clients;

/// <summary>
/// Represents a data service included in Azure Storage.
/// </summary>
public enum AzureStorageService
{
    /// <summary>
    /// A massively scalable object store for text and binary data.
    /// </summary>
    Blob,

    /// <summary>
    /// A messaging store for reliable messaging between application components.
    /// </summary>
    Queue,

    /// <summary>
    /// A NoSQL store for schemaless storage of structured data.
    /// </summary>
    Table,
}
