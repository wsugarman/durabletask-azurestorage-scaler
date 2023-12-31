// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Keda.Scaler.DurableTask.AzureStorage.DataAnnotations;

internal sealed class FileExistsAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);

        if (value is not null)
        {
            if (value is not string filePath)
                return new ValidationResult(SRF.Format(SRF.InvalidMemberTypeFormat, "string", value.GetType().Name), [validationContext.MemberName!]);

            if (!File.Exists(filePath))
                return new ValidationResult(SRF.Format(SRF.FileNotFoundFormat, filePath), [validationContext.MemberName!]);
        }

        return ValidationResult.Success;
    }
}
