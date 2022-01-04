// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using DurableTask.AzureStorage;
using DurableTask.AzureStorage.Monitoring;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Keda.Scaler.DurableTask.AzureStorage.Common;
using Keda.Scaler.DurableTask.AzureStorage.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Auth;
using Moq;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Provider;

[TestClass]
public class PerformanceMonitorFactoryTest
{
    [TestMethod]
    public void CtorExceptions()
    {
        CreateMonitor createMonitor = (c, s) => null!;
        ITokenCredentialFactory credentialFactory = Mock.Of<ITokenCredentialFactory>();
        IProcessEnvironment environment = CurrentEnvironment.Instance;
        ILoggerFactory loggerFactory = NullLoggerFactory.Instance;

        Assert.ThrowsException<ArgumentNullException>(() => new PerformanceMonitorFactory(null!, credentialFactory, environment, loggerFactory));
        Assert.ThrowsException<ArgumentNullException>(() => new PerformanceMonitorFactory(null!, environment, loggerFactory));
        Assert.ThrowsException<ArgumentNullException>(() => new PerformanceMonitorFactory(credentialFactory, null!, loggerFactory));
        Assert.ThrowsException<ArgumentNullException>(() => new PerformanceMonitorFactory(credentialFactory, environment, null!));
    }

    [TestMethod]
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "TokenCredential has a bug.")]
    public async Task CreateAsync()
    {
        ScalerMetadata metadata;
        CreateMonitor createFactory;
        PerformanceMonitorFactory factory;
        PerformanceMonitorDecorator actual;
        DisconnectedPerformanceMonitor monitor = new Mock<DisconnectedPerformanceMonitor>(
            MockBehavior.Strict,
            "UseDevelopmentStorage=true",
            "UnitTestTaskHub").Object;

        using CancellationTokenSource tokenSource = new CancellationTokenSource();
        TokenCredential credential = new TokenCredential("ABC");

        Mock<ITokenCredentialFactory> mockCredentialFactory = new Mock<ITokenCredentialFactory>(MockBehavior.Strict);
        mockCredentialFactory
            .Setup(f => f.CreateAsync(PerformanceMonitorFactory.StorageAccountResource, AzureAuthorityHosts.AzurePublicCloud, tokenSource.Token))
            .ReturnsAsync(credential);

        // Null metadata
        factory = new PerformanceMonitorFactory(mockCredentialFactory.Object, CurrentEnvironment.Instance, NullLoggerFactory.Instance);
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => factory.CreateAsync(null!, tokenSource.Token).AsTask()).ConfigureAwait(false);

        // Connection string
        metadata = new ScalerMetadata
        {
            Connection = "UseDevelopmentStorage=true",
            MaxMessageLatencyMilliseconds = 500,
            TaskHubName = "UnitTestTaskHub",
        };

        createFactory = (c, s) =>
        {
                // Assert dev account
                Assert.AreEqual("http://127.0.0.1:10000/devstoreaccount1", c.BlobEndpoint.AbsoluteUri);
            Assert.IsFalse(c.Credentials.IsToken);
            AssertSettings(NullLoggerFactory.Instance, TimeSpan.FromMilliseconds(500), "UnitTestTaskHub", s);

            return monitor;
        };

        factory = new PerformanceMonitorFactory(createFactory, mockCredentialFactory.Object, CurrentEnvironment.Instance, NullLoggerFactory.Instance);
        actual = (PerformanceMonitorDecorator)await factory.CreateAsync(metadata, tokenSource.Token).ConfigureAwait(false);
        Assert.IsFalse(actual.HasTokenCredential);
        Assert.AreSame(monitor, actual.ToDisconnectedPerformanceMonitor());

        // Managed identity
        metadata = new ScalerMetadata
        {
            AccountName = "unitteststorage",
            MaxMessageLatencyMilliseconds = 500,
            TaskHubName = "UnitTestTaskHub",
            UseAAdPodIdentity = true,
        };

        createFactory = (c, s) =>
        {
            Assert.AreEqual("https://unitteststorage.blob.core.windows.net/", c.BlobEndpoint.AbsoluteUri);
            Assert.IsTrue(c.Credentials.IsToken);
            AssertSettings(NullLoggerFactory.Instance, TimeSpan.FromMilliseconds(500), "UnitTestTaskHub", s);

            return monitor;
        };

        factory = new PerformanceMonitorFactory(createFactory, mockCredentialFactory.Object, CurrentEnvironment.Instance, NullLoggerFactory.Instance);
        actual = (PerformanceMonitorDecorator)await factory.CreateAsync(metadata, tokenSource.Token).ConfigureAwait(false);
        Assert.IsTrue(actual.HasTokenCredential);
        Assert.AreSame(monitor, actual.ToDisconnectedPerformanceMonitor());
    }

    private static void AssertSettings(
        ILoggerFactory loggerFactory,
        TimeSpan pollingInterval,
        string taskHubName,
        AzureStorageOrchestrationServiceSettings actual)
    {
        Assert.AreSame(loggerFactory, actual.LoggerFactory);
        Assert.AreEqual(pollingInterval, actual.MaxQueuePollingInterval);
        Assert.AreEqual(taskHubName, actual.TaskHubName);
    }
}
