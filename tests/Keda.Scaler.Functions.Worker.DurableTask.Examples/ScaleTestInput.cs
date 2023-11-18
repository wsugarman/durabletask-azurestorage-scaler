// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;

namespace Keda.Scaler.Functions.Worker.DurableTask.Examples;

/// <summary>
/// Represents the input for a scale test orchestration.
/// </summary>
public sealed class ScaleTestInput
{
    /// <summary>
    /// Gets or sets the number of activities to be created by the orchestration.
    /// </summary>
    /// <value>The non-negativ number of activities.</value>
    public int ActivityCount { get; set; }

    /// <summary>
    /// Gets or sets the amount of time each activity should take to execute.
    /// </summary>
    /// <value>The duration of each activity.</value>
    public TimeSpan ActivityTime { get; set; }
}
