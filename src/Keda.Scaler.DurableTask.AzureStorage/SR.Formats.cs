// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Globalization;
using System.Text;

namespace Keda.Scaler.DurableTask.AzureStorage;

internal static class SRF
{
    /// <inheritdoc cref="SR.FileNotFoundFormat"/>
    public static CompositeFormat FileNotFoundFormat { get; } = CompositeFormat.Parse(SR.FileNotFoundFormat);

    /// <inheritdoc cref="SR.InvalidConnectionEnvironmentVariableFormat"/>
    public static CompositeFormat InvalidConnectionEnvironmentVariableFormat { get; } = CompositeFormat.Parse(SR.InvalidConnectionEnvironmentVariableFormat);

    /// <inheritdoc cref="SR.InvalidMemberTypeFormat"/>
    public static CompositeFormat InvalidMemberTypeFormat { get; } = CompositeFormat.Parse(SR.InvalidMemberTypeFormat);

    /// <inheritdoc cref="SR.MissingMemberFormat"/>
    public static CompositeFormat MissingMemberFormat { get; } = CompositeFormat.Parse(SR.MissingMemberFormat);

    /// <inheritdoc cref="SR.UnknownValueFormat"/>
    public static CompositeFormat UnknownValueFormat { get; } = CompositeFormat.Parse(SR.UnknownValueFormat);

    public static string Format<TArg0>(CompositeFormat format, TArg0 arg0)
        => string.Format(CultureInfo.CurrentCulture, format, arg0);

    public static string Format<TArg0, TArg1>(CompositeFormat format, TArg0 arg0, TArg1 arg1)
        => string.Format(CultureInfo.CurrentCulture, format, arg0, arg1);
}
