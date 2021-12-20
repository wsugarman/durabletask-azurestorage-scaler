// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Keda.Scaler.DurableTask.AzureStorage.Extensions
{
    internal static class IValidatableObjectExtensions
    {
        public static T EnsureValidated<T>(this T obj, IServiceProvider? serviceProvider = null)
            where T : IValidatableObject
        {
            if (obj is null)
                throw new ArgumentNullException(nameof(obj));

            List<ArgumentException> errors = obj
                .Validate(new ValidationContext(obj, serviceProvider, null))
                .Select(x => new ArgumentException(x.ErrorMessage))
                .ToList();


            if (errors.Count > 0)
                throw new AggregateException(errors);

            return obj;
        }
    }
}
