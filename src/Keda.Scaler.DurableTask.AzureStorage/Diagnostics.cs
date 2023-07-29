// Copyright © William Sugarman.
// Licensed under the MIT License.

namespace Keda.Scaler.DurableTask.AzureStorage;

internal static class Diagnostics
{
    // Reuse prefix from the Durable Task framework
    public const string DefaultLoggerCategory = "DurableTask.AzureStorage.Keda";

    public const string SecurityLoggerCategory = DefaultLoggerCategory + ".Security";
}
