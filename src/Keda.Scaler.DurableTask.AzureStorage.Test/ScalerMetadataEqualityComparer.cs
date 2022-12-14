// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Keda.Scaler.DurableTask.AzureStorage.Test;

internal sealed class ScalerMetadataEqualityComparer : IEqualityComparer<ScalerMetadata>
{
    public static IEqualityComparer<ScalerMetadata> Instance { get; } = new ScalerMetadataEqualityComparer();

    public bool Equals(ScalerMetadata? x, ScalerMetadata? y)
    {
        if (x is null)
            return y is null;
        else if (y is null)
            return false;

        return x.AccountName == y.AccountName
            && x.Cloud == y.Cloud
            && x.Connection == y.Connection
            && x.ConnectionFromEnv == y.ConnectionFromEnv
            && x.MaxMessageLatencyMilliseconds == y.MaxMessageLatencyMilliseconds
            && x.ScaleIncrement == y.ScaleIncrement
            && x.TaskHubName == y.TaskHubName
            && x.UseManagedIdentity == y.UseManagedIdentity;
    }

    public int GetHashCode([DisallowNull] ScalerMetadata obj)
    {
        if (obj is null)
            throw new ArgumentNullException(nameof(obj));

        return HashCode.Combine(
            obj.AccountName,
            obj.Cloud,
            obj.Connection,
            obj.ConnectionFromEnv,
            obj.MaxMessageLatencyMilliseconds,
            obj.ScaleIncrement,
            obj.TaskHubName,
            obj.UseManagedIdentity);
    }
}
