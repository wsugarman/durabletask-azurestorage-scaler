// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Keda.Scaler.DurableTask.AzureStorage.Clients;

namespace Keda.Scaler.DurableTask.AzureStorage.Test;

internal sealed class AzureStorageAccountInfoEqualityComparer : IEqualityComparer<AzureStorageAccountOptions>
{
    public static IEqualityComparer<AzureStorageAccountOptions> Instance { get; } = new AzureStorageAccountInfoEqualityComparer();

    public bool Equals(AzureStorageAccountOptions? x, AzureStorageAccountOptions? y)
    {
        if (x is null)
            return y is null;
        else if (y is null)
            return false;

        return x.AccountName == y.AccountName
            && x.ClientId == y.ClientId
            && x.Cloud?.AuthorityHost == y.Cloud?.AuthorityHost
            && x.Cloud?.StorageSuffix == y.Cloud?.StorageSuffix
            && x.ConnectionString == y.ConnectionString
            && x.Credential == y.Credential;
    }

    public int GetHashCode([DisallowNull] AzureStorageAccountOptions obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        return HashCode.Combine(
            obj.AccountName,
            obj.ClientId,
            HashCode.Combine(obj.Cloud?.AuthorityHost, obj.Cloud?.StorageSuffix),
            obj.ConnectionString,
            obj.Credential);
    }
}
