// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Integration.K8s;

internal sealed class KubernetesOptions
{
    public const string DefaultSectionName = "Kubernetes";

    public string? ConfigPath { get; set; }

    [Required]
    public string? Context { get; set; }
}
