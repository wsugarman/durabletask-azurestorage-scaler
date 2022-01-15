// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using k8s;
using k8s.Models;

namespace Keda.Scaler.DurableTask.AzureStorage.Kubernetes;

// Kubernetes.Client has not opted into a nullable aware context,
// and as such its APIs are not suited to non-nullable reference types.
#nullable disable

internal sealed class V1ScaledObject : IKubernetesObject, IKubernetesObject<V1ObjectMeta>, ISpec<V1ScaledObjectSpec>, IValidate
{
    public string ApiVersion { get; set; }

    public string Kind { get; set; }

    public V1ObjectMeta Metadata { get; set; }

    public V1ScaledObjectSpec Spec { get; set; }

    public void Validate()
    {
        Metadata?.Validate();
        Spec?.Validate();
    }
}

#nullable enable
