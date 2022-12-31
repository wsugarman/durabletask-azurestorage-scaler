// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Integration.K8s;

[SuppressMessage("Microsoft.Performance", "CA1812:Avoid uninstantiated internal classes.", Justification = "This class is instantiated via dependency injection.")]
internal sealed class FunctionDeploymentOptions
{
    public const string DefaultSectionName = "Function";

    [Required]
    public string? Name { get; set; }

    [Required]
    public string? Namespace { get; set; }
}
