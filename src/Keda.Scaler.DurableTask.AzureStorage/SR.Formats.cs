// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Globalization;
using System.Text;

namespace Keda.Scaler.DurableTask.AzureStorage;

internal static class SRF
{
    /// <inheritdoc cref="SR.EmptyOrWhiteSpaceFormat"/>
    public static CompositeFormat EmptyOrWhiteSpace { get; } = CompositeFormat.Parse(SR.EmptyOrWhiteSpaceFormat);

    /// <inheritdoc cref="SR.FileNotFoundFormat"/>
    public static CompositeFormat FileNotFound { get; } = CompositeFormat.Parse(SR.FileNotFoundFormat);

    /// <inheritdoc cref="SR.IdentityConnectionOnlyPropertyFormat"/>
    public static CompositeFormat IdentityConnectionOnlyProperty { get; } = CompositeFormat.Parse(SR.IdentityConnectionOnlyPropertyFormat);

    /// <inheritdoc cref="SR.InvalidConnectionEnvironmentVariableFormat"/>
    public static CompositeFormat InvalidConnectionEnvironmentVariable { get; } = CompositeFormat.Parse(SR.InvalidConnectionEnvironmentVariableFormat);

    /// <inheritdoc cref="SR.InvalidMemberTypeFormat"/>
    public static CompositeFormat InvalidMemberTypeFormat { get; } = CompositeFormat.Parse(SR.InvalidMemberTypeFormat);

    /// <inheritdoc cref="SR.MissingMemberFormat"/>
    public static CompositeFormat MissingMemberFormat { get; } = CompositeFormat.Parse(SR.MissingMemberFormat);

    /// <inheritdoc cref="SR.MissingPrivateCloudPropertyFormat"/>
    public static CompositeFormat MissingPrivateCloudProperty { get; } = CompositeFormat.Parse(SR.MissingPrivateCloudPropertyFormat);

    /// <inheritdoc cref="SR.PrivateCloudOnlyPropertyFormat"/>
    public static CompositeFormat PrivateCloudOnlyProperty { get; } = CompositeFormat.Parse(SR.PrivateCloudOnlyPropertyFormat);

    /// <inheritdoc cref="SR.ServiceUriOnlyPropertyFormat"/>
    public static CompositeFormat ServiceUriOnlyProperty { get; } = CompositeFormat.Parse(SR.ServiceUriOnlyPropertyFormat);

    /// <inheritdoc cref="SR.UnknownCloudValueFormat"/>
    public static CompositeFormat UnknownCloudValue { get; } = CompositeFormat.Parse(SR.UnknownCloudValueFormat);

    /// <inheritdoc cref="SR.UnknownValueFormat"/>
    public static CompositeFormat UnknownValueFormat { get; } = CompositeFormat.Parse(SR.UnknownValueFormat);

    public static string Format<TArg0>(CompositeFormat format, TArg0 arg0)
        => string.Format(CultureInfo.CurrentCulture, format, arg0);

    public static string Format<TArg0, TArg1>(CompositeFormat format, TArg0 arg0, TArg1 arg1)
        => string.Format(CultureInfo.CurrentCulture, format, arg0, arg1);
}
