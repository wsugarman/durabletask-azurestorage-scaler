// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Keda.Scaler.DurableTask.AzureStorage.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Extensions;

[TestClass]
public class IValidatableObjectExtensionsTest
{
    [TestMethod]
    public void EnsureValidated()
    {
        IServiceProvider services = new ServiceCollection().AddSingleton(Epoch.Unix).BuildServiceProvider();

        // Null object
        Assert.ThrowsException<ArgumentNullException>(() => IValidatableObjectExtensions.EnsureValidated<Example>(null!));

        // Single error
        Example single = new Example { EvenNumber = 3 };
        Assert.ThrowsException<ValidationException>(() => single.EnsureValidated(services));

        // Multiple validation results
        Example multiple = new Example { EvenNumber = 101, RecentTime = DateTime.MinValue + TimeSpan.FromHours(1) };
        AggregateException actual = Assert.ThrowsException<AggregateException>(() => multiple.EnsureValidated(services));
        Assert.AreEqual(2, actual.InnerExceptions.Count);
        Assert.IsTrue(actual.InnerExceptions.All(x => x.GetType() == typeof(ValidationException)));
    }

    private sealed class Example : IValidatableObject
    {
        public int EvenNumber { get; init; }

        public DateTime RecentTime { get; init; } = DateTime.UtcNow;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EvenNumber % 2 != 0)
                yield return new ValidationResult($"{EvenNumber} is not even!");

            if (RecentTime < validationContext.GetRequiredService<Epoch>().Value)
                yield return new ValidationResult($"{RecentTime} takes place before the epoch.");
        }
    }

    private sealed class Epoch
    {
        public static Epoch Unix { get; } = new Epoch { Value = DateTime.UnixEpoch };

        public DateTime Value { get; init; }
    }
}
