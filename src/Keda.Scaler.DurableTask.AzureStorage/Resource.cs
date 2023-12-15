// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Globalization;
using System.Text;

namespace Keda.Scaler.DurableTask.AzureStorage;

internal static class Resource
{
    public static string Format(CompositeFormat format, object? arg0)
        => string.Format(CultureInfo.CurrentCulture, format, arg0);

    public static string Format(CompositeFormat format, object? arg0, object? arg1)
        => string.Format(CultureInfo.CurrentCulture, format, arg0, arg1);
}
