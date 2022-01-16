// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Globalization;
using System.Resources;

namespace Keda.Scaler.DurableTask.AzureStorage;

static partial class SR
{
    public static string InvalidApiVersionFormat => ExceptionsManager.GetString(nameof(InvalidApiVersionFormat), CultureInfo.CurrentUICulture)!;

    public static string InvalidK8sResourceFormat => ExceptionsManager.GetString(nameof(InvalidK8sResourceFormat), CultureInfo.CurrentUICulture)!;

    public static string JsonParseFormat => ExceptionsManager.GetString(nameof(JsonParseFormat), CultureInfo.CurrentUICulture)!;

    public static string UnknownScaleActionFormat => ExceptionsManager.GetString(nameof(UnknownScaleActionFormat), CultureInfo.CurrentUICulture)!;

    private static readonly ResourceManager ExceptionsManager = new ResourceManager("Keda.Scaler.DurableTask.AzureStorage.Resources.Exceptions", typeof(SR).Assembly);
}
