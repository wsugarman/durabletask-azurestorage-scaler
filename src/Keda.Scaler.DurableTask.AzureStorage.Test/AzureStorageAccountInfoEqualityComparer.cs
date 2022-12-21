// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Keda.Scaler.DurableTask.AzureStorage.Accounts;

namespace Keda.Scaler.DurableTask.AzureStorage.Test;

internal sealed class AzureStorageAccountInfoEqualityComparer : IEqualityComparer<AzureStorageAccountInfo>
{
    public static IEqualityComparer<AzureStorageAccountInfo> Instance { get; } = new AzureStorageAccountInfoEqualityComparer();

    public bool Equals(AzureStorageAccountInfo? x, AzureStorageAccountInfo? y)
    {
        if (x is null)
            return y is null;
        else if (y is null)
            return false;

        return x.AccountName == y.AccountName
            && x.ClientId == y.ClientId
            && x.CloudEnvironment == y.CloudEnvironment
            && x.ConnectionString == y.ConnectionString
            && x.Credential == y.Credential;
    }

    public int GetHashCode([DisallowNull] AzureStorageAccountInfo obj)
    {
        if (obj is null)
            throw new ArgumentNullException(nameof(obj));

        return HashCode.Combine(
            obj.AccountName,
            obj.ClientId,
            obj.CloudEnvironment,
            obj.ConnectionString,
            obj.Credential);
    }
}
