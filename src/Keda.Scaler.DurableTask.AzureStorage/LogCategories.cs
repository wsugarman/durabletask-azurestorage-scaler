// Copyright © William Sugarman.
// Licensed under the MIT License.

namespace Keda.Scaler.DurableTask.AzureStorage;

internal static class LogCategories
{
    // Reuse prefix from the Durable Task framework
    public const string Default = "DurableTask.AzureStorage.Keda";

    public const string Security = Default + ".Security";
}
