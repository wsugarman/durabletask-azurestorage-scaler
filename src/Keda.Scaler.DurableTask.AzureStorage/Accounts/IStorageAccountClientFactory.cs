// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Keda.Scaler.DurableTask.AzureStorage.Clients;

namespace Keda.Scaler.DurableTask.AzureStorage.Accounts;

/// <summary>
/// Represents a factory for clients used to communicate with the different Azure Storage services.
/// </summary>
/// <typeparam name="T">The type of the service client.</typeparam>
public interface IStorageAccountClientFactory<T>
{
    /// <summary>
    /// Retrieves a service client of type <typeparamref name="T"/> for the the given <paramref name="accountInfo"/>.
    /// </summary>
    /// <param name="accountInfo">The information for an Azure Storage account.</param>
    /// <returns>An instance of type <typeparamref name="T"/> representing the service client..</returns>
    /// <exception cref="ArgumentException"><paramref name="accountInfo"/> is missing information.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="accountInfo"/> is <see langword="null"/>.</exception>
    T GetServiceClient(AzureStorageAccountOptions accountInfo);
}
