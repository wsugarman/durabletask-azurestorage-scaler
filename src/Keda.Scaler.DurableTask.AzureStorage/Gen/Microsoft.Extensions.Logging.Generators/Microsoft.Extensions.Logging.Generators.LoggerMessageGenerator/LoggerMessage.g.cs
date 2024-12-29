﻿// <auto-generated/>
#nullable enable

namespace Keda.Scaler.DurableTask.AzureStorage.Certificates
{
    partial class Log
    {
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Logging.Generators", "9.0.11.2809")]
        private static readonly global::System.Action<global::Microsoft.Extensions.Logging.ILogger, global::System.String, global::System.String, global::System.Exception?> __ReloadedCustomCertificateAuthorityCallback =
            global::Microsoft.Extensions.Logging.LoggerMessage.Define<global::System.String, global::System.String>(global::Microsoft.Extensions.Logging.LogLevel.Information, new global::Microsoft.Extensions.Logging.EventId(13, nameof(ReloadedCustomCertificateAuthority)), "The custom CA certificate at '{Path}' has been reloaded with thumbprint {Thumbprint}.", new global::Microsoft.Extensions.Logging.LogDefineOptions() { SkipEnabledCheck = true }); 

        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Logging.Generators", "9.0.11.2809")]
        public static partial void ReloadedCustomCertificateAuthority(this global::Microsoft.Extensions.Logging.ILogger logger, global::System.String path, global::System.String thumbprint)
        {
            if (logger.IsEnabled(global::Microsoft.Extensions.Logging.LogLevel.Information))
            {
                __ReloadedCustomCertificateAuthorityCallback(logger, path, thumbprint, null);
            }
        }
    }
}
namespace Keda.Scaler.DurableTask.AzureStorage.Interceptors
{
    partial class Log
    {
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Logging.Generators", "9.0.11.2809")]
        private static readonly global::System.Action<global::Microsoft.Extensions.Logging.ILogger, global::System.Exception?> __CaughtUnhandledExceptionCallback =
            global::Microsoft.Extensions.Logging.LoggerMessage.Define(global::Microsoft.Extensions.Logging.LogLevel.Critical, new global::Microsoft.Extensions.Logging.EventId(1, nameof(CaughtUnhandledException)), "Caught unhandled exception!", new global::Microsoft.Extensions.Logging.LogDefineOptions() { SkipEnabledCheck = true }); 

        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Logging.Generators", "9.0.11.2809")]
        public static partial void CaughtUnhandledException(this global::Microsoft.Extensions.Logging.ILogger logger, global::System.Exception exception)
        {
            if (logger.IsEnabled(global::Microsoft.Extensions.Logging.LogLevel.Critical))
            {
                __CaughtUnhandledExceptionCallback(logger, exception);
            }
        }
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Logging.Generators", "9.0.11.2809")]
        private static readonly global::System.Action<global::Microsoft.Extensions.Logging.ILogger, global::System.Exception?> __ReceivedInvalidInputCallback =
            global::Microsoft.Extensions.Logging.LoggerMessage.Define(global::Microsoft.Extensions.Logging.LogLevel.Error, new global::Microsoft.Extensions.Logging.EventId(2, nameof(ReceivedInvalidInput)), "Request contains invalid input.", new global::Microsoft.Extensions.Logging.LogDefineOptions() { SkipEnabledCheck = true }); 

        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Logging.Generators", "9.0.11.2809")]
        public static partial void ReceivedInvalidInput(this global::Microsoft.Extensions.Logging.ILogger logger, global::System.Exception exception)
        {
            if (logger.IsEnabled(global::Microsoft.Extensions.Logging.LogLevel.Error))
            {
                __ReceivedInvalidInputCallback(logger, exception);
            }
        }
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Logging.Generators", "9.0.11.2809")]
        private static readonly global::System.Action<global::Microsoft.Extensions.Logging.ILogger, global::System.Exception?> __DetectedRequestCancellationCallback =
            global::Microsoft.Extensions.Logging.LoggerMessage.Define(global::Microsoft.Extensions.Logging.LogLevel.Warning, new global::Microsoft.Extensions.Logging.EventId(3, nameof(DetectedRequestCancellation)), "RPC operation canceled.", new global::Microsoft.Extensions.Logging.LogDefineOptions() { SkipEnabledCheck = true }); 

        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Logging.Generators", "9.0.11.2809")]
        public static partial void DetectedRequestCancellation(this global::Microsoft.Extensions.Logging.ILogger logger, global::System.OperationCanceledException exception)
        {
            if (logger.IsEnabled(global::Microsoft.Extensions.Logging.LogLevel.Warning))
            {
                __DetectedRequestCancellationCallback(logger, exception);
            }
        }
    }
}
namespace Keda.Scaler.DurableTask.AzureStorage.TaskHubs
{
    partial class Log
    {
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Logging.Generators", "9.0.11.2809")]
        private static readonly global::System.Action<global::Microsoft.Extensions.Logging.ILogger, global::System.String, global::System.Int32, global::System.DateTimeOffset, global::System.String, global::System.Exception?> __FoundTaskHubBlobCallback =
            global::Microsoft.Extensions.Logging.LoggerMessage.Define<global::System.String, global::System.Int32, global::System.DateTimeOffset, global::System.String>(global::Microsoft.Extensions.Logging.LogLevel.Debug, new global::Microsoft.Extensions.Logging.EventId(4, nameof(FoundTaskHubBlob)), "Found Task Hub '{TaskHubName}' with {Partitions} partitions created at {CreatedTime:O} in blob {TaskHubBlobName}.", new global::Microsoft.Extensions.Logging.LogDefineOptions() { SkipEnabledCheck = true }); 

        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Logging.Generators", "9.0.11.2809")]
        public static partial void FoundTaskHubBlob(this global::Microsoft.Extensions.Logging.ILogger logger, global::System.String taskHubName, global::System.Int32 partitions, global::System.DateTimeOffset createdTime, global::System.String taskHubBlobName)
        {
            if (logger.IsEnabled(global::Microsoft.Extensions.Logging.LogLevel.Debug))
            {
                __FoundTaskHubBlobCallback(logger, taskHubName, partitions, createdTime, taskHubBlobName, null);
            }
        }
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Logging.Generators", "9.0.11.2809")]
        private static readonly global::System.Action<global::Microsoft.Extensions.Logging.ILogger, global::System.String, global::System.String, global::System.String, global::System.Exception?> __CannotFindTaskHubBlobCallback =
            global::Microsoft.Extensions.Logging.LoggerMessage.Define<global::System.String, global::System.String, global::System.String>(global::Microsoft.Extensions.Logging.LogLevel.Warning, new global::Microsoft.Extensions.Logging.EventId(5, nameof(CannotFindTaskHubBlob)), "Cannot find Task Hub '{TaskHubName}' metadata blob '{TaskHubBlobName}' in container '{LeaseContainerName}'.", new global::Microsoft.Extensions.Logging.LogDefineOptions() { SkipEnabledCheck = true }); 

        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Logging.Generators", "9.0.11.2809")]
        public static partial void CannotFindTaskHubBlob(this global::Microsoft.Extensions.Logging.ILogger logger, global::System.String taskHubName, global::System.String taskHubBlobName, global::System.String leaseContainerName)
        {
            if (logger.IsEnabled(global::Microsoft.Extensions.Logging.LogLevel.Warning))
            {
                __CannotFindTaskHubBlobCallback(logger, taskHubName, taskHubBlobName, leaseContainerName, null);
            }
        }
    }
}
namespace Keda.Scaler.DurableTask.AzureStorage.TaskHubs
{
    partial class Log
    {
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Logging.Generators", "9.0.11.2809")]
        private static readonly global::System.Action<global::Microsoft.Extensions.Logging.ILogger, global::System.String, global::System.Exception?> __CouldNotFindControlQueueCallback =
            global::Microsoft.Extensions.Logging.LoggerMessage.Define<global::System.String>(global::Microsoft.Extensions.Logging.LogLevel.Warning, new global::Microsoft.Extensions.Logging.EventId(6, nameof(CouldNotFindControlQueue)), "Could not find control queue '{ControlQueueName}'.", new global::Microsoft.Extensions.Logging.LogDefineOptions() { SkipEnabledCheck = true }); 

        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Logging.Generators", "9.0.11.2809")]
        public static partial void CouldNotFindControlQueue(this global::Microsoft.Extensions.Logging.ILogger logger, global::System.String controlQueueName)
        {
            if (logger.IsEnabled(global::Microsoft.Extensions.Logging.LogLevel.Warning))
            {
                __CouldNotFindControlQueueCallback(logger, controlQueueName, null);
            }
        }
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Logging.Generators", "9.0.11.2809")]
        private static readonly global::System.Action<global::Microsoft.Extensions.Logging.ILogger, global::System.String, global::System.Exception?> __CouldNotFindWorkItemQueueCallback =
            global::Microsoft.Extensions.Logging.LoggerMessage.Define<global::System.String>(global::Microsoft.Extensions.Logging.LogLevel.Warning, new global::Microsoft.Extensions.Logging.EventId(7, nameof(CouldNotFindWorkItemQueue)), "Could not find work item queue '{WorkItemQueueName}'.", new global::Microsoft.Extensions.Logging.LogDefineOptions() { SkipEnabledCheck = true }); 

        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Logging.Generators", "9.0.11.2809")]
        public static partial void CouldNotFindWorkItemQueue(this global::Microsoft.Extensions.Logging.ILogger logger, global::System.String workItemQueueName)
        {
            if (logger.IsEnabled(global::Microsoft.Extensions.Logging.LogLevel.Warning))
            {
                __CouldNotFindWorkItemQueueCallback(logger, workItemQueueName, null);
            }
        }
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Logging.Generators", "9.0.11.2809")]
        private static readonly global::System.Action<global::Microsoft.Extensions.Logging.ILogger, global::System.Int32, global::System.String, global::System.Exception?> __FoundTaskHubQueuesCallback =
            global::Microsoft.Extensions.Logging.LoggerMessage.Define<global::System.Int32, global::System.String>(global::Microsoft.Extensions.Logging.LogLevel.Debug, new global::Microsoft.Extensions.Logging.EventId(8, nameof(FoundTaskHubQueues)), "Found {WorkItemCount} work item messages and the following control queue message counts [{ControlCounts}].", new global::Microsoft.Extensions.Logging.LogDefineOptions() { SkipEnabledCheck = true }); 

        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Logging.Generators", "9.0.11.2809")]
        public static partial void FoundTaskHubQueues(this global::Microsoft.Extensions.Logging.ILogger logger, global::System.Int32 workItemCount, global::System.String controlCounts)
        {
            if (logger.IsEnabled(global::Microsoft.Extensions.Logging.LogLevel.Debug))
            {
                __FoundTaskHubQueuesCallback(logger, workItemCount, controlCounts, null);
            }
        }
    }
}
namespace Keda.Scaler.DurableTask.AzureStorage.TaskHubs
{
    partial class Log
    {
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Logging.Generators", "9.0.11.2809")]
        private static readonly global::System.Action<global::Microsoft.Extensions.Logging.ILogger, global::System.String, global::System.Int32, global::System.String, global::System.Exception?> __FoundTaskHubPartitionsTableCallback =
            global::Microsoft.Extensions.Logging.LoggerMessage.Define<global::System.String, global::System.Int32, global::System.String>(global::Microsoft.Extensions.Logging.LogLevel.Debug, new global::Microsoft.Extensions.Logging.EventId(4, nameof(FoundTaskHubPartitionsTable)), "Found Task Hub '{TaskHubName}' with {Partitions} partitions in table '{TaskHubTableName}'.", new global::Microsoft.Extensions.Logging.LogDefineOptions() { SkipEnabledCheck = true }); 

        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Logging.Generators", "9.0.11.2809")]
        public static partial void FoundTaskHubPartitionsTable(this global::Microsoft.Extensions.Logging.ILogger logger, global::System.String taskHubName, global::System.Int32 partitions, global::System.String taskHubTableName)
        {
            if (logger.IsEnabled(global::Microsoft.Extensions.Logging.LogLevel.Debug))
            {
                __FoundTaskHubPartitionsTableCallback(logger, taskHubName, partitions, taskHubTableName, null);
            }
        }
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Logging.Generators", "9.0.11.2809")]
        private static readonly global::System.Action<global::Microsoft.Extensions.Logging.ILogger, global::System.String, global::System.String, global::System.Exception?> __CannotFindTaskHubPartitionsTableCallback =
            global::Microsoft.Extensions.Logging.LoggerMessage.Define<global::System.String, global::System.String>(global::Microsoft.Extensions.Logging.LogLevel.Warning, new global::Microsoft.Extensions.Logging.EventId(5, nameof(CannotFindTaskHubPartitionsTable)), "Cannot find Task Hub '{TaskHubName}' partitions table blob '{TaskHubTableName}'.", new global::Microsoft.Extensions.Logging.LogDefineOptions() { SkipEnabledCheck = true }); 

        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Logging.Generators", "9.0.11.2809")]
        public static partial void CannotFindTaskHubPartitionsTable(this global::Microsoft.Extensions.Logging.ILogger logger, global::System.String taskHubName, global::System.String taskHubTableName)
        {
            if (logger.IsEnabled(global::Microsoft.Extensions.Logging.LogLevel.Warning))
            {
                __CannotFindTaskHubPartitionsTableCallback(logger, taskHubName, taskHubTableName, null);
            }
        }
    }
}
namespace Keda.Scaler.DurableTask.AzureStorage.TaskHubs
{
    partial class Log
    {
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Logging.Generators", "9.0.11.2809")]
        private static readonly global::System.Action<global::Microsoft.Extensions.Logging.ILogger, global::System.String, global::System.Int64, global::System.Exception?> __ComputedScalerMetricValueCallback =
            global::Microsoft.Extensions.Logging.LoggerMessage.Define<global::System.String, global::System.Int64>(global::Microsoft.Extensions.Logging.LogLevel.Information, new global::Microsoft.Extensions.Logging.EventId(9, nameof(ComputedScalerMetricValue)), "Metric value for Task Hub '{TaskHubName}' is {MetricValue}.", new global::Microsoft.Extensions.Logging.LogDefineOptions() { SkipEnabledCheck = true }); 

        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Logging.Generators", "9.0.11.2809")]
        public static partial void ComputedScalerMetricValue(this global::Microsoft.Extensions.Logging.ILogger logger, global::System.String taskHubName, global::System.Int64 metricValue)
        {
            if (logger.IsEnabled(global::Microsoft.Extensions.Logging.LogLevel.Information))
            {
                __ComputedScalerMetricValueCallback(logger, taskHubName, metricValue, null);
            }
        }
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Logging.Generators", "9.0.11.2809")]
        private static readonly global::System.Action<global::Microsoft.Extensions.Logging.ILogger, global::System.String, global::System.Int64, global::System.Exception?> __ComputedScalerMetricTargetCallback =
            global::Microsoft.Extensions.Logging.LoggerMessage.Define<global::System.String, global::System.Int64>(global::Microsoft.Extensions.Logging.LogLevel.Information, new global::Microsoft.Extensions.Logging.EventId(10, nameof(ComputedScalerMetricTarget)), "Metric target for Task Hub '{TaskHubName}' is {MetricTarget}.", new global::Microsoft.Extensions.Logging.LogDefineOptions() { SkipEnabledCheck = true }); 

        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Logging.Generators", "9.0.11.2809")]
        public static partial void ComputedScalerMetricTarget(this global::Microsoft.Extensions.Logging.ILogger logger, global::System.String taskHubName, global::System.Int64 metricTarget)
        {
            if (logger.IsEnabled(global::Microsoft.Extensions.Logging.LogLevel.Information))
            {
                __ComputedScalerMetricTargetCallback(logger, taskHubName, metricTarget, null);
            }
        }
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Logging.Generators", "9.0.11.2809")]
        private static readonly global::System.Action<global::Microsoft.Extensions.Logging.ILogger, global::System.String, global::System.Exception?> __DetectedActiveTaskHubCallback =
            global::Microsoft.Extensions.Logging.LoggerMessage.Define<global::System.String>(global::Microsoft.Extensions.Logging.LogLevel.Information, new global::Microsoft.Extensions.Logging.EventId(11, nameof(DetectedActiveTaskHub)), "Task Hub '{TaskHubName}' is currently active.", new global::Microsoft.Extensions.Logging.LogDefineOptions() { SkipEnabledCheck = true }); 

        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Logging.Generators", "9.0.11.2809")]
        public static partial void DetectedActiveTaskHub(this global::Microsoft.Extensions.Logging.ILogger logger, global::System.String taskHubName)
        {
            if (logger.IsEnabled(global::Microsoft.Extensions.Logging.LogLevel.Information))
            {
                __DetectedActiveTaskHubCallback(logger, taskHubName, null);
            }
        }
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Logging.Generators", "9.0.11.2809")]
        private static readonly global::System.Action<global::Microsoft.Extensions.Logging.ILogger, global::System.String, global::System.Exception?> __DetectedInactiveTaskHubCallback =
            global::Microsoft.Extensions.Logging.LoggerMessage.Define<global::System.String>(global::Microsoft.Extensions.Logging.LogLevel.Information, new global::Microsoft.Extensions.Logging.EventId(12, nameof(DetectedInactiveTaskHub)), "Task Hub '{TaskHubName}' is not currently active.", new global::Microsoft.Extensions.Logging.LogDefineOptions() { SkipEnabledCheck = true }); 

        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Logging.Generators", "9.0.11.2809")]
        public static partial void DetectedInactiveTaskHub(this global::Microsoft.Extensions.Logging.ILogger logger, global::System.String taskHubName)
        {
            if (logger.IsEnabled(global::Microsoft.Extensions.Logging.LogLevel.Information))
            {
                __DetectedInactiveTaskHubCallback(logger, taskHubName, null);
            }
        }
    }
}