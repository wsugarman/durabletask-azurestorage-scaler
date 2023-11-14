// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Globalization;
using System.Resources;

namespace Keda.Scaler.DurableTask.AzureStorage;

internal static partial class SR
{
    public static string InternalErrorMessage => ResponseResourceManager.GetString(nameof(InternalErrorMessage), CultureInfo.CurrentUICulture)!;

    private static readonly ResourceManager ResponseResourceManager = new("Keda.Scaler.DurableTask.AzureStorage.Resources.Responses", typeof(SR).Assembly);
}
