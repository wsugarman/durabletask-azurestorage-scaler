// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Integration;

internal static partial class Log
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Started '{Orchestration}' instance '{InstanceId}'.")]
    public static partial void StartedOrchestration(this ILogger logger, string orchestration, string instanceId);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Waiting for instance '{InstanceId}' to complete.")]
    public static partial void WaitingForOrchestration(this ILogger logger, string instanceId);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Current status of instance '{InstanceId}' is '{Status}'.")]
    public static partial void ObservedOrchestrationStatus(this ILogger logger, string instanceId, OrchestrationRuntimeStatus status);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "Instance '{InstanceId}' reached terminal status '{Status}'.")]
    public static partial void ObservedOrchestrationCompletion(this ILogger logger, string instanceId, OrchestrationRuntimeStatus? status);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "Waiting for scale down to {Target} replicas.")]
    public static partial void MonitoringWorkerScaleDown(this ILogger logger, int target);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Information,
        Message = "Waiting for scale up to at least {Target} replicas.")]
    public static partial void MonitoringWorkerScaleUp(this ILogger logger, int target);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Information,
        Message = "Current scale for deployment '{Deployment}' in namespace '{Namespace}' is {Status}/{Spec}...")]
    public static partial void ObservedKubernetesDeploymentScale(this ILogger logger, string deployment, string @namespace, int status, int spec);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Information,
        Message = "Terminated instance '{InstanceId}.'")]
    public static partial void TerminatedOrchestration(this ILogger logger, string instanceId);

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Warning,
        Message = "Error encountered when terminating instance '{InstanceId}.'")]
    public static partial void FailedTerminatingOrchestration(this ILogger logger, Exception exception, string instanceId);
}
