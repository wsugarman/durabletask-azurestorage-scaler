// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Keda.Scaler.DurableTask.AzureStorage.Extensions;

internal static class IValidatableObjectExtensions
{
    [return: NotNull]
    public static T EnsureValidated<T>([DisallowNull] this T obj, IServiceProvider? serviceProvider = null)
        where T : IValidatableObject
    {
        if (obj is null)
            throw new ArgumentNullException(nameof(obj));

        List<ValidationException> errors = obj
            .Validate(new ValidationContext(obj, serviceProvider, null))
            .Select(x => new ValidationException(x, null, null))
            .ToList();

        if (errors.Count > 0)
            throw errors.Count == 1 ? errors.Single() : new AggregateException(errors);

        return obj;
    }
}
