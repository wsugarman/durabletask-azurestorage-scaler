// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Globalization;

namespace Keda.Scaler.DurableTask.AzureStorage;

internal static partial class SR
{
    public static string Format(string format, object? arg0)
        => string.Format(CultureInfo.CurrentCulture, format, arg0);

    public static string Format(string format, object? arg0, object? arg1)
        => string.Format(CultureInfo.CurrentCulture, format, arg0, arg1);

    public static string Format(string format, params object?[] args)
        => string.Format(CultureInfo.CurrentCulture, format, args);
}
