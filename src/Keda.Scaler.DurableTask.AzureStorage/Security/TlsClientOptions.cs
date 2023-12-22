// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Keda.Scaler.DurableTask.AzureStorage.DataAnnotations;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

internal class TlsClientOptions : IValidatableObject
{
    public const string DefaultKey = "Security:Transport:Client";

    [FileExists]
    public string? CaCertificatePath { get; set; }

    public bool ValidateCertificate { get; set; } = true;

    IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
    {
        if (CaCertificatePath is not null && !ValidateCertificate)
            yield return new ValidationResult(SR.InvalidTlsClientValidation, [nameof(CaCertificatePath), nameof(ValidateCertificate)]);
    }
}
