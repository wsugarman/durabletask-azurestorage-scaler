// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.ComponentModel.DataAnnotations;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Integration;

internal sealed class ScaleTestOptions
{
    public const string DefaultSectionName = "Scaling";

    [Range(1, int.MaxValue)]
    public int MaxActivitiesPerWorker { get; set; } = 10;

    [Range(typeof(TimeSpan), "00:00:00", "00:05:00", ConvertValueInInvariantCulture = true, ParseLimitsInInvariantCulture = true)]
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(30);

    [Range(typeof(TimeSpan), "00:00:30", "00:10:00", ConvertValueInInvariantCulture = true, ParseLimitsInInvariantCulture = true)]
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(2);
}
