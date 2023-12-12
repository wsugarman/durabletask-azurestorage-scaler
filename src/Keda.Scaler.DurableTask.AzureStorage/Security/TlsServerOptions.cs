// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Keda.Scaler.DurableTask.AzureStorage.ComponentModel.DataAnnotations;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

internal class TlsServerOptions : IValidatableObject
{
    public const string DefaultKey = "Security:Transport:Server";

    [MemberNotNullWhen(true, nameof(CertificatePath))]
    public bool EnforceTls => !string.IsNullOrWhiteSpace(CertificatePath);

    [FileExists]
    public string? CertificatePath { get; set; }

    [FileExists]
    public string? KeyPath { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(CertificatePath) && !string.IsNullOrWhiteSpace(KeyPath))
            yield return new ValidationResult(SR.MissingCertificateMessage);
    }
}
