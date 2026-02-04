// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Integration;

[SuppressMessage("Microsoft.Performance", "CA1812:Avoid uninstantiated internal classes.", Justification = "This class is instantiated via dependency injection.")]
internal sealed class ScaleTestOptions : IValidatableObject
{
    public const string DefaultSectionName = "Scaling";

    [Range(typeof(TimeSpan), "00:00:00", "00:05:00", ConvertValueInInvariantCulture = true, ParseLimitsInInvariantCulture = true)]
    public TimeSpan ActivityDuration { get; set; } = TimeSpan.FromMinutes(1);

    [Range(typeof(TimeSpan), "00:00:01", "00:05:00", ConvertValueInInvariantCulture = true, ParseLimitsInInvariantCulture = true)]
    public TimeSpan LoggingInterval { get; set; } = TimeSpan.FromSeconds(60);

    [Range(1, int.MaxValue)]
    public int MaxActivitiesPerWorker { get; set; } = 10;

    [Range(0, int.MaxValue)]
    public int MinReplicas { get; set; } = 0;

    [Range(typeof(TimeSpan), "00:00:01", "00:00:10", ConvertValueInInvariantCulture = true, ParseLimitsInInvariantCulture = true)]
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(2);

    public int PollingIntervalsPerLog => (int)LoggingInterval.TotalSeconds / (int)PollingInterval.TotalSeconds;

    IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
    {
        if (LoggingInterval.TotalSeconds % PollingInterval.TotalSeconds != 0)
            yield return new ValidationResult("Polling interval must be a multiple of the logging interval.");
    }
}
