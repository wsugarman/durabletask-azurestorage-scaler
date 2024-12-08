// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Azure.Core;

namespace Keda.Scaler.DurableTask.AzureStorage.Clients;

/// <summary>
/// Represents a factory for creating Azure Storage service clients based on the context for a given request.
/// </summary>
/// <typeparam name="T">The type of the service client.</typeparam>
public abstract class AzureStorageAccountClientFactory<T>
{
    /// <summary>
    /// Gets the Azure Storage service associated with produced clients.
    /// </summary>
    /// <value>
    /// <see cref="AzureStorageService.Blob"/>, <see cref="AzureStorageService.Queue"/>,
    /// or <see cref="AzureStorageService.Table"/>.
    /// </value>
    protected abstract AzureStorageService Service { get; }

    /// <summary>
    /// Creates a service client based on the given account options.
    /// </summary>
    /// <param name="options">The options associated with the current request.</param>
    /// <returns>The corresponding Azure Storage service client.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public T GetServiceClient(AzureStorageAccountOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            Uri serviceUri = AzureStorageServiceUri.Create(options.AccountName!, Service, options.EndpointSuffix!);
            return CreateServiceClient(serviceUri, options.TokenCredential!);
        }
        else
        {
            return CreateServiceClient(options.ConnectionString);
        }
    }

    /// <summary>
    /// Creates a service client based on the given connection string.
    /// </summary>
    /// <param name="connectionString">An Azure Storage connection string.</param>
    /// <returns>The corresponding Azure Storage service client.</returns>
    protected abstract T CreateServiceClient(string connectionString);

    /// <summary>
    /// Creates a service client based on the given service URI.
    /// </summary>
    /// <param name="serviceUri">An Azure Storage service URI.</param>
    /// <param name="credential">A token credential for authenticating the client.</param>
    /// <returns>The corresponding Azure Storage service client.</returns>
    protected abstract T CreateServiceClient(Uri serviceUri, TokenCredential credential);
}
