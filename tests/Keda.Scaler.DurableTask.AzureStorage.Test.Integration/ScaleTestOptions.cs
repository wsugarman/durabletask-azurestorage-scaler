// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Integration;

[SuppressMessage("Microsoft.Performance", "CA1812:Avoid uninstantiated internal classes.", Justification = "This class is instantiated via dependency injection.")]
internal sealed class ScaleTestOptions
{
    public const string DefaultSectionName = "Scaling";

    [Range(typeof(TimeSpan), "00:00:00", "00:05:00", ConvertValueInInvariantCulture = true, ParseLimitsInInvariantCulture = true)]
    public TimeSpan ActivityDuration { get; set; } = TimeSpan.FromMinutes(1);

    [Range(1, int.MaxValue)]
    public int MaxActivitiesPerWorker { get; set; } = 10;

    [Range(0, int.MaxValue)]
    public int MinReplicas { get; set; } = 0;

    [Range(typeof(TimeSpan), "00:00:00", "00:05:00", ConvertValueInInvariantCulture = true, ParseLimitsInInvariantCulture = true)]
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(30);

    [Range(typeof(TimeSpan), "00:00:30", "00:10:00", ConvertValueInInvariantCulture = true, ParseLimitsInInvariantCulture = true)]
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(2);
}
