// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using k8s;

namespace Keda.Scaler.DurableTask.AzureStorage.Kubernetes;

#nullable disable

internal sealed class V1ScaledObjectSpec : IValidate
{
    public V1ScaleTargetRef ScaleTargetRef { get; set; }

    public void Validate()
    {
        if (ScaleTargetRef is null)
            throw new ArgumentNullException(nameof(ScaleTargetRef));

        ScaleTargetRef.Validate();
    }
}

#nullable enable
