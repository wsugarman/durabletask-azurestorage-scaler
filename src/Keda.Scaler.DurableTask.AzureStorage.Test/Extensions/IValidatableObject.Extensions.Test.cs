// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keda.Scaler.DurableTask.AzureStorage.Extensions.Test
{
    [TestClass]
    public class IValidatableObjectExtensionsTest
    {
        [TestMethod]
        public void EnsureValidated()
        {
            IServiceProvider services = new ServiceCollection().AddSingleton(Epoch.Unix).BuildServiceProvider();

            // Null object
            Assert.ThrowsException<ArgumentNullException>(() => IValidatableObjectExtensions.EnsureValidated<Example>(null!));

            // Multiple validation results
            Example example = new Example { EvenNumber = 101, RecentTime = DateTime.MinValue + TimeSpan.FromHours(1) };
            AggregateException actual = Assert.ThrowsException<AggregateException>(() => example.EnsureValidated(services));
            Assert.AreEqual(2, actual.InnerExceptions.Count);
            Assert.IsTrue(actual.InnerExceptions.All(x => x.GetType() == typeof(ArgumentException)));
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
}
