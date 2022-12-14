// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.AzureStorage;
using DurableTask.AzureStorage.Monitoring;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Keda.Scaler.DurableTask.AzureStorage.Common;
using Keda.Scaler.DurableTask.AzureStorage.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;

namespace Keda.Scaler.DurableTask.AzureStorage.Provider;

internal delegate DisconnectedPerformanceMonitor CreateMonitor(CloudStorageAccount cloudStorageAccount, AzureStorageOrchestrationServiceSettings settings);

internal sealed class PerformanceMonitorFactory : IPerformanceMonitorFactory
{
    internal const string StorageAccountResource = "https://storage.azure.com/";

    private readonly CreateMonitor _monitorFactory;
    private readonly ITokenCredentialFactory _credentialFactory;
    private readonly IProcessEnvironment _environment;
    private readonly ILoggerFactory _loggerFactory;

    internal PerformanceMonitorFactory(
        ITokenCredentialFactory credentialFactory,
        IProcessEnvironment environment,
        ILoggerFactory loggerFactory)
        : this((c, s) => new DisconnectedPerformanceMonitor(c, s), credentialFactory, environment, loggerFactory)
    { }

    internal PerformanceMonitorFactory(
        CreateMonitor monitorFactory,
        ITokenCredentialFactory credentialFactory,
        IProcessEnvironment environment,
        ILoggerFactory loggerFactory)
    {
        _monitorFactory = monitorFactory ?? throw new ArgumentNullException(nameof(monitorFactory));
        _credentialFactory = credentialFactory ?? throw new ArgumentNullException(nameof(credentialFactory));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public async ValueTask<IPerformanceMonitor> CreateAsync(ScalerMetadata metadata, CancellationToken cancellationToken = default)
    {
        if (metadata is null)
            throw new ArgumentNullException(nameof(metadata));

        AzureStorageOrchestrationServiceSettings settings = new AzureStorageOrchestrationServiceSettings
        {
            LoggerFactory = _loggerFactory,
            MaxQueuePollingInterval = TimeSpan.FromMilliseconds(metadata.MaxMessageLatencyMilliseconds),
            TaskHubName = metadata.TaskHubName,
        };

        if (metadata.AccountName is null)
        {
            return new PerformanceMonitorDecorator(
                _monitorFactory(
                    CloudStorageAccount.Parse(metadata.ResolveConnectionString(_environment)),
                    settings));
        }
        else
        {
            CloudEndpoints endpoints = CloudEndpoints.ForEnvironment(metadata.CloudEnvironment);
            TokenCredential tokenCredential = await _credentialFactory.CreateAsync(
                StorageAccountResource,
                endpoints.AuthorityHost,
                cancellationToken).ConfigureAwait(false);

            DisconnectedPerformanceMonitor performanceMonitor = _monitorFactory(
                new CloudStorageAccount(
                    new StorageCredentials(tokenCredential),
                    metadata.AccountName,
                    endpoints.StorageSuffix,
                    useHttps: true),
                settings);

            return new PerformanceMonitorDecorator(performanceMonitor, tokenCredential);
        }
    }
}
