// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Keda.Scaler.DurableTask.AzureStorage.Cloud;
using Keda.Scaler.DurableTask.AzureStorage.Test;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Keda.Scaler.DurableTask.AzureStorage.Services.Test
{
    [TestClass]
    public class DurableTaskAzureStorageScalerServiceTest
    {
        private const string MetricName = "TestMetric";

        private readonly Mock<IDurableTaskAzureStorageScaler> _mockScaler = new Mock<IDurableTaskAzureStorageScaler>();
        private readonly IServiceProvider _serviceProvider;

        public DurableTaskAzureStorageScalerServiceTest()
        {
            _mockScaler.Setup(s => s.MetricName).Returns(MetricName);

            ServiceCollection services = new ServiceCollection();
            services.AddSingleton(_mockScaler.Object);
            _serviceProvider = services.BuildServiceProvider();
        }

        [TestMethod]
        public void CtorExceptions()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new DurableTaskAzureStorageScalerService(null!));
            Assert.ThrowsException<InvalidOperationException>(() => new DurableTaskAzureStorageScalerService(new ServiceCollection().BuildServiceProvider()));
        }

        [TestMethod]
        public async Task IsActive()
        {
            ScalerMetadata metadata = new ScalerMetadata
            {
                AccountName = "unitteststorage",
                Cloud = nameof(CloudEnvironment.AzurePublicCloud),
                MaxMessageLatencyMilliseconds = 500,
                ScaleIncrement = 2,
                TaskHubName = "UnitTestTaskHub",
                UseAAdPodIdentity = true,
            };

            using CancellationTokenSource tokenSource = new CancellationTokenSource();

            ServerCallContext context = new MockServerCallContext(tokenSource.Token);
            _mockScaler
                .Setup(s => s.IsActiveAsync(It.Is(metadata, ScalerMetadataEqualityComparer.Instance), tokenSource.Token))
                .ReturnsAsync(true);

            DurableTaskAzureStorageScalerService service = new DurableTaskAzureStorageScalerService(_serviceProvider);
            IsActiveResponse actual = await service.IsActive(CreateScaledObjectRef(metadata), context).ConfigureAwait(false);
            Assert.IsTrue(actual.Result);
        }

        [TestMethod]
        public async Task GetMetricSpec()
        {
            const long targetValue = 42;

            ScalerMetadata metadata = new ScalerMetadata
            {
                AccountName = "unitteststorage",
                Cloud = nameof(CloudEnvironment.AzurePublicCloud),
                MaxMessageLatencyMilliseconds = 500,
                ScaleIncrement = 2,
                TaskHubName = "UnitTestTaskHub",
                UseAAdPodIdentity = true,
            };

            using CancellationTokenSource tokenSource = new CancellationTokenSource();

            ServerCallContext context = new MockServerCallContext(tokenSource.Token);
            _mockScaler
                .Setup(s => s.GetMetricSpecAsync(It.Is(metadata, ScalerMetadataEqualityComparer.Instance), tokenSource.Token))
                .ReturnsAsync(targetValue);

            DurableTaskAzureStorageScalerService service = new DurableTaskAzureStorageScalerService(_serviceProvider);
            GetMetricSpecResponse response = await service.GetMetricSpec(CreateScaledObjectRef(metadata), context).ConfigureAwait(false);

            MetricSpec actual = response.MetricSpecs.Single();
            Assert.AreEqual(MetricName, actual.MetricName);
            Assert.AreEqual(targetValue, actual.TargetSize);
        }

        [TestMethod]
        public async Task GetMetrics()
        {
            const long metricValue = 17;

            DeploymentReference deployment = new DeploymentReference("unit-test-func", "durable-task");
            ScalerMetadata metadata = new ScalerMetadata
            {
                AccountName = "unitteststorage",
                Cloud = nameof(CloudEnvironment.AzurePublicCloud),
                MaxMessageLatencyMilliseconds = 500,
                ScaleIncrement = 2,
                TaskHubName = "UnitTestTaskHub",
                UseAAdPodIdentity = true,
            };

            using CancellationTokenSource tokenSource = new CancellationTokenSource();

            ServerCallContext context = new MockServerCallContext(tokenSource.Token);
            _mockScaler
                .Setup(s => s.GetMetricValueAsync(deployment, It.Is(metadata, ScalerMetadataEqualityComparer.Instance), tokenSource.Token))
                .ReturnsAsync(metricValue);

            DurableTaskAzureStorageScalerService service = new DurableTaskAzureStorageScalerService(_serviceProvider);
            GetMetricsResponse response = await service.GetMetrics(CreateGetMetricsRequest(deployment, metadata), context).ConfigureAwait(false);

            MetricValue actual = response.MetricValues.Single();
            Assert.AreEqual(MetricName, actual.MetricName);
            Assert.AreEqual(metricValue, actual.MetricValue_);
        }

        private static ScaledObjectRef CreateScaledObjectRef(ScalerMetadata metadata)
            => CreateScaledObjectRef(default, metadata);

        private static GetMetricsRequest CreateGetMetricsRequest(DeploymentReference deployment, ScalerMetadata metadata)
            => new GetMetricsRequest { MetricName = MetricName, ScaledObjectRef = CreateScaledObjectRef(deployment, metadata) };

        private static ScaledObjectRef CreateScaledObjectRef(DeploymentReference deployment, ScalerMetadata metadata)
        {
            ScaledObjectRef result = deployment == default
                ? new ScaledObjectRef()
                : new ScaledObjectRef
                {
                    Name = deployment.Name,
                    Namespace = deployment.Namespace,
                };

            if (metadata.AccountName is not null)
                result.ScalerMetadata[nameof(ScalerMetadata.AccountName)] = metadata.AccountName;

            if (metadata.Connection is not null)
                result.ScalerMetadata[nameof(ScalerMetadata.Connection)] = metadata.Connection;

            if (metadata.ConnectionFromEnv is not null)
                result.ScalerMetadata[nameof(ScalerMetadata.ConnectionFromEnv)] = metadata.ConnectionFromEnv;

            if (metadata.TaskHubName is not null)
                result.ScalerMetadata[nameof(ScalerMetadata.TaskHubName)] = metadata.TaskHubName;

            result.ScalerMetadata[nameof(ScalerMetadata.Cloud)] = metadata.Cloud;
            result.ScalerMetadata[nameof(ScalerMetadata.MaxMessageLatencyMilliseconds)] = metadata.MaxMessageLatencyMilliseconds.ToString(CultureInfo.InvariantCulture);
            result.ScalerMetadata[nameof(ScalerMetadata.ScaleIncrement)] = metadata.ScaleIncrement.ToString(CultureInfo.InvariantCulture);
            result.ScalerMetadata[nameof(ScalerMetadata.UseAAdPodIdentity)] = metadata.UseAAdPodIdentity.ToString(CultureInfo.InvariantCulture);

            return result;
        }

        private sealed class MockServerCallContext : ServerCallContext
        {
            protected override CancellationToken CancellationTokenCore { get; }

            public MockServerCallContext(CancellationToken cancellationToken)
                => CancellationTokenCore = cancellationToken;

            #region Not Implmented

            protected override string MethodCore => throw new NotImplementedException();

            protected override string HostCore => throw new NotImplementedException();

            protected override string PeerCore => throw new NotImplementedException();

            protected override DateTime DeadlineCore => throw new NotImplementedException();

            protected override Metadata RequestHeadersCore => throw new NotImplementedException();

            protected override Metadata ResponseTrailersCore => throw new NotImplementedException();

            protected override Status StatusCore { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            protected override WriteOptions WriteOptionsCore { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            protected override AuthContext AuthContextCore => throw new NotImplementedException();

            protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions options) => throw new NotImplementedException();

            protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => throw new NotImplementedException();

            #endregion
        }
    }
}
