// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Integration.K8s;

internal sealed class FunctionDeploymentOptions
{
    public const string DefaultSectionName = "Function";

    [Required]
    public string? Name { get; set; }

    [Required]
    public string? Namespace { get; set; }
}
