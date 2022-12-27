// Copyright © William Sugarman.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Integration;

internal static class OrchestrationRuntimeStatusExtensions
{
    public static bool IsTerminal(this OrchestrationRuntimeStatus status)
        => status is OrchestrationRuntimeStatus.Unknown
            or OrchestrationRuntimeStatus.Completed
            or OrchestrationRuntimeStatus.Failed
            or OrchestrationRuntimeStatus.Canceled
            or OrchestrationRuntimeStatus.Terminated;
}
