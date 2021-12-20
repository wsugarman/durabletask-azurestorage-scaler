// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.AzureStorage.Monitoring;
using k8s;
using k8s.Models;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Keda.Scaler.DurableTask.AzureStorage.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Keda.Scaler.DurableTask.AzureStorage.Services.Test
{
    [TestClass]
    public class DurableTaskAzureStorageScalerTest
    {
        [TestMethod]
        public void CtorExceptions()
        {
            IKubernetes kubernetes = Mock.Of<IKubernetes>();
            ITokenCredentialFactory tokenCredentialFactory = Mock.Of<ITokenCredentialFactory>();
            IEnvironment environment = CurrentEnvironment.Instance;
            ILoggerFactory loggerFactory = NullLoggerFactory.Instance;

            Assert.ThrowsException<ArgumentNullException>(() => new DurableTaskAzureStorageScaler(null!, tokenCredentialFactory, environment, loggerFactory));
            Assert.ThrowsException<ArgumentNullException>(() => new DurableTaskAzureStorageScaler(kubernetes, null!, environment, loggerFactory));
            Assert.ThrowsException<ArgumentNullException>(() => new DurableTaskAzureStorageScaler(kubernetes, tokenCredentialFactory, null!, loggerFactory));
            Assert.ThrowsException<ArgumentNullException>(() => new DurableTaskAzureStorageScaler(kubernetes, tokenCredentialFactory, environment, null!));
        }

        [TestMethod]
        public async Task GetScaleMetricSpecAsync()
        {
            IKubernetes kubernetes = Mock.Of<IKubernetes>();
            ITokenCredentialFactory tokenCredentialFactory = Mock.Of<ITokenCredentialFactory>();
            IEnvironment environment = CurrentEnvironment.Instance;
            ILoggerFactory loggerFactory = NullLoggerFactory.Instance;

            DurableTaskAzureStorageScaler scaler = new DurableTaskAzureStorageScaler(kubernetes, tokenCredentialFactory, environment, loggerFactory);

            // Null metadata
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => scaler.GetScaleMetricSpecAsync(null!).AsTask()).ConfigureAwait(false);

            // Non-null metadata
            Assert.AreEqual(DurableTaskAzureStorageScaler.MetricSpecValue, await scaler.GetScaleMetricSpecAsync(new ScalerMetadata()).ConfigureAwait(false));
        }

        [DataTestMethod]
        [DataRow()]
        public async Task GetIncreasingScaleMetricValueAsync(ScaleAction action, int ratio, int replicas,  bool useAAdPodIdentity)
        {
            DeploymentReference deployment = new DeploymentReference("unit-test-func", "durable-task");
            ScalerMetadata metadata = useAAdPodIdentity
                ? new ScalerMetadata
                {
                    AccountName = "unitteststorage",
                    Cloud = CloudEnvironment.AzurePublicCloud,
                    MaxMessageLatencyMilliseconds = 1000,
                    ScaleIncrement = ratio,
                    TaskHubName = "UnitTestTaskHub",
                    UseAAdPodIdentity = true,
                }
                : new ScalerMetadata
                {
                    Connection = "UseDevelopmentStorage=true",
                    MaxMessageLatencyMilliseconds = 1000,
                    ScaleIncrement = ratio,
                    TaskHubName = "UnitTestTaskHub",
                    UseAAdPodIdentity = false,
                };

            V1Scale scale = new V1Scale { Status = new V1ScaleStatus { Replicas = replicas } };

            using CancellationTokenSource tokenSource = new CancellationTokenSource();

            Mock<IKubernetes> k8sMock = new Mock<IKubernetes>(MockBehavior.Strict);
            k8sMock
                .Setup(k => k.ReadNamespacedDeploymentScaleAsync(deployment.Name, deployment.Namespace, false, tokenSource.Token))
                .ReturnsAsync(scale);



            ITokenCredentialFactory tokenCredentialFactory = Mock.Of<ITokenCredentialFactory>();
            IEnvironment environment = CurrentEnvironment.Instance;
            ILoggerFactory loggerFactory = NullLoggerFactory.Instance;

            DurableTaskAzureStorageScaler scaler = new DurableTaskAzureStorageScaler(kubernetes, tokenCredentialFactory, environment, loggerFactory);

            // Null metadata
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => scaler.GetScaleMetricValueAsync(default, null!).AsTask()).ConfigureAwait(false);

            // Non-null metadata
            



            long value = await scaler.GetScaleMetricValueAsync(deployment, metadata).ConfigureAwait(false);
        }
    }
}
