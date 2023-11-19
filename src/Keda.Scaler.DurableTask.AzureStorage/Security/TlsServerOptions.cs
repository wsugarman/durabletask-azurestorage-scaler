// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

internal class TlsServerOptions : IValidatableObject
{
    public const string DefaultKey = "Security:Transport:Server";

    public string? CertificatePath { get; set; }

    public string? KeyPath { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);

        if (!string.IsNullOrWhiteSpace(CertificatePath))
        {
            if (!File.Exists(CertificatePath))
                yield return new ValidationResult(SR.Format(SR.FileNotFoundFormat, nameof(CertificatePath), CertificatePath));
            else if (!string.IsNullOrWhiteSpace(KeyPath) && !File.Exists(CertificatePath))
                yield return new ValidationResult(SR.Format(SR.FileNotFoundFormat, nameof(KeyPath), KeyPath));
        }
        else if (!string.IsNullOrWhiteSpace(KeyPath))
        {
            yield return new ValidationResult(SR.MissingCertificateMessage);
        }
    }
}
