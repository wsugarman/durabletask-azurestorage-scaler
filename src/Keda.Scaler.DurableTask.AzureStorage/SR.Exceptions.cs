// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Globalization;
using System.Resources;

namespace Keda.Scaler.DurableTask.AzureStorage;

internal static partial class SR
{
    public static string InvalidPartitionCountFormat => ExceptionsManager.GetString(nameof(InvalidPartitionCountFormat), CultureInfo.CurrentUICulture)!;

    public static string MissingMemberFormat => ExceptionsManager.GetString(nameof(MissingMemberFormat), CultureInfo.CurrentUICulture)!;

    private static readonly ResourceManager ExceptionsManager = new ResourceManager("Keda.Scaler.DurableTask.AzureStorage.Resources.Exceptions", typeof(SR).Assembly);
}
