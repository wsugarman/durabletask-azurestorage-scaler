// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.ComponentModel.DataAnnotations;

namespace Keda.Scaler.DurableTask.AzureStorage.ComponentModel.DataAnnotations;

internal static class ObjectValidator
{
    public static void ThrowIfInvalid(object obj, IServiceProvider? serviceProvider)
        => Validator.ValidateObject(obj, new ValidationContext(obj, serviceProvider, null), validateAllProperties: true);

    public static T ThrowIfInvalid<T>(this T obj, IServiceProvider? serviceProvider)
        where T : IValidatableObject
    {
        Validator.ValidateObject(obj, new ValidationContext(obj, serviceProvider, null), validateAllProperties: true);
        return obj;
    }
}
