// Copyright © William Sugarman.
// Licensed under the MIT License.

namespace Keda.Scaler.DurableTask.AzureStorage.Clients;

internal static class CloudEnvironment
{
    public const string Private = nameof(Private);

    public const string AzurePublicCloud = nameof(AzurePublicCloud);

    public const string AzureUSGovernmentCloud = nameof(AzureUSGovernmentCloud);

    public const string AzureChinaCloud = nameof(AzureChinaCloud);
}
