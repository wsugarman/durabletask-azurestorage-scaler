// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;

namespace Keda.Scaler.DurableTask.AzureStorage.Certificates;

internal class CaCertificateFileOptions
{
    [Required]
    public string Path { get; set; } = default!;

    [Range(0, 1000 * 60)]
    public int ReloadDelayMs { get; set; } = 250;
}
