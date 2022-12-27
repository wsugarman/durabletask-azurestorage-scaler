// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Integration.K8s;

internal sealed class KubernetesOptions
{
    public const string DefaultSectionName = "Kubernetes";

    [Required]
    public string? ConfigPath { get; set; }

    public string? Context { get; set; }
}
