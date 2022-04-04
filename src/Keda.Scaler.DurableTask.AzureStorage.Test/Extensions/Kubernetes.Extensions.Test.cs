// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Keda.Scaler.DurableTask.AzureStorage.Extensions;
using Keda.Scaler.DurableTask.AzureStorage.Kubernetes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using ScalerKubernetesExtensions = Keda.Scaler.DurableTask.AzureStorage.Extensions.KubernetesExtensions;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Extensions;

[TestClass]
public class KubernetesExtensionsTest
{
    [TestMethod]
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Request and Response not set for HttpOperationResponse.")]
    public async Task ReadNamespacedCustomObjectScaleAsync()
    {
        using CancellationTokenSource tokenSource = new CancellationTokenSource();
        Mock<IKubernetes> k8sMock = new Mock<IKubernetes>(MockBehavior.Strict);

        // Invalid input
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => ScalerKubernetesExtensions
            .ReadNamespacedCustomObjectScaleAsync(null!, "foo", "default", "apps", "v1", "deployment", tokenSource.Token).AsTask()).ConfigureAwait(false);
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => k8sMock.Object
            .ReadNamespacedCustomObjectScaleAsync(null!, "default", "apps", "v1", "deployment", tokenSource.Token).AsTask()).ConfigureAwait(false);
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => k8sMock.Object
            .ReadNamespacedCustomObjectScaleAsync("foo", null!, "apps", "v1", "deployment", tokenSource.Token).AsTask()).ConfigureAwait(false);
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => k8sMock.Object
            .ReadNamespacedCustomObjectScaleAsync("foo", "default", null!, "v1", "deployment", tokenSource.Token).AsTask()).ConfigureAwait(false);
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => k8sMock.Object
            .ReadNamespacedCustomObjectScaleAsync("foo", "default", "apps", null!, "deployment", tokenSource.Token).AsTask()).ConfigureAwait(false);
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => k8sMock.Object
            .ReadNamespacedCustomObjectScaleAsync("foo", "default", "apps", "v1", null!, tokenSource.Token).AsTask()).ConfigureAwait(false);

        // null
        k8sMock
            .Setup(k => k.GetNamespacedCustomObjectScaleWithHttpMessagesAsync("apps", "v1", "default", "deployments", "foo", null, tokenSource.Token))
            .ReturnsAsync(new HttpOperationResponse<object> { Body = null! });

        await Assert.ThrowsExceptionAsync<JsonException>(() => k8sMock.Object
            .ReadNamespacedCustomObjectScaleAsync("foo", "default", "apps", "v1", "deployment", tokenSource.Token).AsTask()).ConfigureAwait(false);

        // Invalid type (should be JSON)
        k8sMock
            .Setup(k => k.GetNamespacedCustomObjectScaleWithHttpMessagesAsync("apps", "v1", "default", "deployments", "bar", null, tokenSource.Token))
            .ReturnsAsync(new HttpOperationResponse<object> { Body = new V1Scale() });

        await Assert.ThrowsExceptionAsync<JsonException>(() => k8sMock.Object
            .ReadNamespacedCustomObjectScaleAsync("bar", "default", "apps", "v1", "deployment", tokenSource.Token).AsTask()).ConfigureAwait(false);

        // Invalid json kind
        k8sMock
            .Setup(k => k.GetNamespacedCustomObjectScaleWithHttpMessagesAsync("apps", "v1", "default", "deployments", "baz", null, tokenSource.Token))
            .ReturnsAsync(new HttpOperationResponse<object> { Body = JsonSerializer.Deserialize<object>("[ 1, 2, 3 ]")! });

        await Assert.ThrowsExceptionAsync<JsonException>(() => k8sMock.Object
            .ReadNamespacedCustomObjectScaleAsync("baz", "default", "apps", "v1", "deployment", tokenSource.Token).AsTask()).ConfigureAwait(false);

        // Valid
        k8sMock
            .Setup(k => k.GetNamespacedCustomObjectScaleWithHttpMessagesAsync("apps", "v1", "default", "deployments", "good", null, tokenSource.Token))
            .ReturnsAsync(
                new HttpOperationResponse<object>
                {
                    Body = JsonSerializer.Deserialize<object>(@"
{
  ""apiVersion"": ""autoscaling/v1"",
  ""kind"": ""Scale"",
  ""metadata"": {
      ""name"": ""good"",
      ""namespace"": ""default""
  },
  ""status"": {
    ""replicas"": 5
  }
}
",
                    ScalerKubernetesExtensions.JsonSerializerOptions)!,
                });

        V1Scale actual = await k8sMock.Object.ReadNamespacedCustomObjectScaleAsync("good", "default", "apps", "v1", "deployment", tokenSource.Token).ConfigureAwait(false);
        Assert.AreEqual("autoscaling/v1", actual.ApiVersion);
        Assert.AreEqual("Scale", actual.Kind);
        Assert.AreEqual("good", actual.Metadata.Name);
        Assert.AreEqual("default", actual.Metadata.NamespaceProperty);
        Assert.AreEqual(5, actual.Status.Replicas);
    }

    [TestMethod]
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Request and Response not set for HttpOperationResponse.")]
    public async Task ReadNamespacedScaledObjectAsync()
    {
        using CancellationTokenSource tokenSource = new CancellationTokenSource();
        Mock<IKubernetes> k8sMock = new Mock<IKubernetes>(MockBehavior.Strict);

        // Invalid input
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => ScalerKubernetesExtensions
            .ReadNamespacedScaledObjectAsync(null!, "foo", "default", tokenSource.Token).AsTask()).ConfigureAwait(false);
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => k8sMock.Object
            .ReadNamespacedScaledObjectAsync(null!, "default", tokenSource.Token).AsTask()).ConfigureAwait(false);
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => k8sMock.Object
            .ReadNamespacedScaledObjectAsync("foo", null!, tokenSource.Token).AsTask()).ConfigureAwait(false);

        // null
        k8sMock
            .Setup(k => k.GetNamespacedCustomObjectWithHttpMessagesAsync("keda.sh", "v1alpha1", "default", "ScaledObjects", "foo", null, tokenSource.Token))
            .ReturnsAsync(new HttpOperationResponse<object> { Body = null! });

        await Assert.ThrowsExceptionAsync<JsonException>(() => k8sMock.Object
            .ReadNamespacedScaledObjectAsync("foo", "default", tokenSource.Token).AsTask()).ConfigureAwait(false);

        // Invalid type (should be JSON)
        k8sMock
            .Setup(k => k.GetNamespacedCustomObjectWithHttpMessagesAsync("keda.sh", "v1alpha1", "default", "ScaledObjects", "bar", null, tokenSource.Token))
            .ReturnsAsync(new HttpOperationResponse<object> { Body = new V1ScaledObject() });

        await Assert.ThrowsExceptionAsync<JsonException>(() => k8sMock.Object
            .ReadNamespacedScaledObjectAsync("bar", "default", tokenSource.Token).AsTask()).ConfigureAwait(false);

        // Invalid json kind
        k8sMock
            .Setup(k => k.GetNamespacedCustomObjectWithHttpMessagesAsync("keda.sh", "v1alpha1", "default", "ScaledObjects", "baz", null, tokenSource.Token))
            .ReturnsAsync(new HttpOperationResponse<object> { Body = JsonSerializer.Deserialize<object>("[ 1, 2, 3 ]")! });

        await Assert.ThrowsExceptionAsync<JsonException>(() => k8sMock.Object
            .ReadNamespacedScaledObjectAsync("baz", "default", tokenSource.Token).AsTask()).ConfigureAwait(false);

        // Invalid values
        k8sMock
            .Setup(k => k.GetNamespacedCustomObjectWithHttpMessagesAsync("keda.sh", "v1alpha1", "default", "ScaledObjects", "bad", null, tokenSource.Token))
            .ReturnsAsync(
                new HttpOperationResponse<object>
                {
                    Body = JsonSerializer.Deserialize<object>(@"
{
  ""apiVersion"": ""keda.sh/v1alpha1"",
  ""kind"": ""ScaledObject"",
  ""metadata"": {
      ""name"": ""bad"",
      ""namespace"": ""default""
  },
  ""spec"": {
    ""scaleTargetRef"": {
      ""apiVersion"": ""custom.sh/v2beta/typo"",
      ""kind"": ""beehive"",
      ""name"": ""buzz""
    },
    ""triggers"": [{
        ""type"": ""external"",
        ""metadata"": {
          ""scalerAddress"": ""durabletask-azurestorage-external-scaler.keda:1234"",
          ""taskHubName"": ""mytaskhub""
        }
      }
    ]
  }
}
",
                        ScalerKubernetesExtensions.JsonSerializerOptions)!,
                });

        await Assert.ThrowsExceptionAsync<ArgumentException>(() => k8sMock.Object
            .ReadNamespacedScaledObjectAsync("bad", "default", tokenSource.Token).AsTask()).ConfigureAwait(false);

        // Valid
        k8sMock
            .Setup(k => k.GetNamespacedCustomObjectWithHttpMessagesAsync("keda.sh", "v1alpha1", "honeycomb", "ScaledObjects", "good", null, tokenSource.Token))
            .ReturnsAsync(
                new HttpOperationResponse<object>
                {
                    Body = JsonSerializer.Deserialize<object>(@"
{
  ""apiVersion"": ""keda.sh/v1alpha1"",
  ""kind"": ""ScaledObject"",
  ""metadata"": {
      ""name"": ""good"",
      ""namespace"": ""honeycomb""
  },
  ""spec"": {
    ""scaleTargetRef"": {
      ""apiVersion"": ""custom.sh/v2beta"",
      ""kind"": ""beehive"",
      ""name"": ""buzz""
    },
    ""triggers"": [{
        ""type"": ""external"",
        ""metadata"": {
          ""scalerAddress"": ""durabletask-azurestorage-external-scaler.keda:1234"",
          ""taskHubName"": ""mytaskhub""
        }
      }
    ]
  }
}
",
                        ScalerKubernetesExtensions.JsonSerializerOptions)!,
                });

        V1ScaledObject actual = await k8sMock.Object.ReadNamespacedScaledObjectAsync("good", "honeycomb", tokenSource.Token).ConfigureAwait(false);
        Assert.AreEqual("keda.sh/v1alpha1", actual.ApiVersion);
        Assert.AreEqual("ScaledObject", actual.Kind);
        Assert.AreEqual("good", actual.Metadata.Name);
        Assert.AreEqual("honeycomb", actual.Metadata.NamespaceProperty);
        Assert.AreEqual("custom.sh/v2beta", actual.Spec.ScaleTargetRef.ApiVersion);
        Assert.AreEqual("beehive", actual.Spec.ScaleTargetRef.Kind);
        Assert.AreEqual("buzz", actual.Spec.ScaleTargetRef.Name);
    }
}
