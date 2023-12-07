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
                return new ValidationResult(SR.Format(SR.InvalidPropertyTypeFormat, "string"), [validationContext.MemberName!]);

            if (!File.Exists(filePath))
                return new ValidationResult(SR.FileNotFoundMessage, [validationContext.MemberName!]);
        }

        return ValidationResult.Success;
    }
}
