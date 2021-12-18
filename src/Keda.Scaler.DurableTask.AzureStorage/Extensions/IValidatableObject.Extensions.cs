// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Keda.Scaler.DurableTask.AzureStorage.Extensions
{
    internal static class ValidationExtensions
    {
        public static void EnsureValidated(this IValidatableObject obj)
        {
            if (obj is null)
                throw new ArgumentNullException(nameof(obj));

            List<ArgumentException> errors = obj.Validate(new ValidationContext(obj)).Select(x => new ArgumentException(x.ErrorMessage)).ToList();
            if (errors.Count == 1)
                throw errors[0];
            else if (errors.Count > 0)
                throw new AggregateException(errors);
        }
    }
}
