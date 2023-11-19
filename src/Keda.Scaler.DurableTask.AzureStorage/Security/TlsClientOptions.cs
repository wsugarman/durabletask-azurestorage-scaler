// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

internal class TlsClientOptions : IValidatableObject
{
    public const string DefaultKey = "Security:Transport:Client";

    public string? CaCertificatePath { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);

        if (!string.IsNullOrWhiteSpace(CaCertificatePath) && !File.Exists(CaCertificatePath))
            yield return new ValidationResult(SR.Format(SR.FileNotFoundFormat, nameof(CaCertificatePath), CaCertificatePath));
    }
}
