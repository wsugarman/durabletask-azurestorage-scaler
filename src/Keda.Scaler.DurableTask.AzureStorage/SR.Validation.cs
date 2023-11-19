// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Globalization;
using System.Resources;

namespace Keda.Scaler.DurableTask.AzureStorage;

internal static partial class SR
{
    public static string AadOnlyFieldFormat => ValidationResourceManager.GetString(nameof(AadOnlyFieldFormat), CultureInfo.CurrentUICulture)!;

    public static string AadRequiredFieldFormat => ValidationResourceManager.GetString(nameof(AadRequiredFieldFormat), CultureInfo.CurrentUICulture)!;

    public static string BlankConnectionVarFormat => ValidationResourceManager.GetString(nameof(BlankConnectionVarFormat), CultureInfo.CurrentUICulture)!;

    public static string FileNotFoundFormat => ValidationResourceManager.GetString(nameof(FileNotFoundFormat), CultureInfo.CurrentUICulture)!;

    public static string InvalidAadFieldFormat => ValidationResourceManager.GetString(nameof(InvalidAadFieldFormat), CultureInfo.CurrentUICulture)!;

    public static string MissingCertificateMessage => ValidationResourceManager.GetString(nameof(MissingCertificateMessage), CultureInfo.CurrentUICulture)!;

    public static string OptionalBlankValueFormat => ValidationResourceManager.GetString(nameof(OptionalBlankValueFormat), CultureInfo.CurrentUICulture)!;

    public static string PositiveValueFormat => ValidationResourceManager.GetString(nameof(PositiveValueFormat), CultureInfo.CurrentUICulture)!;

    public static string PrivateCloudOnlyFieldFormat => ValidationResourceManager.GetString(nameof(PrivateCloudOnlyFieldFormat), CultureInfo.CurrentUICulture)!;

    public static string PrivateCloudRequiredFieldFormat => ValidationResourceManager.GetString(nameof(PrivateCloudRequiredFieldFormat), CultureInfo.CurrentUICulture)!;

    public static string RequiredBlankValueFormat => ValidationResourceManager.GetString(nameof(RequiredBlankValueFormat), CultureInfo.CurrentUICulture)!;

    public static string UnknownValueFormat => ValidationResourceManager.GetString(nameof(UnknownValueFormat), CultureInfo.CurrentUICulture)!;

    private static readonly ResourceManager ValidationResourceManager = new("Keda.Scaler.DurableTask.AzureStorage.Resources.Validation", typeof(SR).Assembly);
}
