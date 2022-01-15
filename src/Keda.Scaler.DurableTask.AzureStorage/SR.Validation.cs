// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Globalization;
using System.Resources;

namespace Keda.Scaler.DurableTask.AzureStorage;

static partial class SR
{
    public static string AadOnlyFieldFormat => ValidationResourceManager.GetString(nameof(AadOnlyFieldFormat), CultureInfo.CurrentUICulture)!;

    public static string AadRequiredFieldFormat => ValidationResourceManager.GetString(nameof(AadRequiredFieldFormat), CultureInfo.CurrentUICulture)!;

    public static string BlankConnectionVarFormat => ValidationResourceManager.GetString(nameof(BlankConnectionVarFormat), CultureInfo.CurrentUICulture)!;

    public static string InvalidAadFieldFormat => ValidationResourceManager.GetString(nameof(InvalidAadFieldFormat), CultureInfo.CurrentUICulture)!;

    public static string NegativeValueFormat => ValidationResourceManager.GetString(nameof(NegativeValueFormat), CultureInfo.CurrentUICulture)!;

    public static string OptionalBlankValueFormat => ValidationResourceManager.GetString(nameof(OptionalBlankValueFormat), CultureInfo.CurrentUICulture)!;

    public static string RequiredBlankValueFormat => ValidationResourceManager.GetString(nameof(RequiredBlankValueFormat), CultureInfo.CurrentUICulture)!;

    public static string UnknownValueFormat => ValidationResourceManager.GetString(nameof(UnknownValueFormat), CultureInfo.CurrentUICulture)!;

    public static string ValueTooBigFormat => ValidationResourceManager.GetString(nameof(ValueTooBigFormat), CultureInfo.CurrentUICulture)!;

    public static string ValueTooSmallFormat => ValidationResourceManager.GetString(nameof(ValueTooSmallFormat), CultureInfo.CurrentUICulture)!;

    private static readonly ResourceManager ValidationResourceManager = new ResourceManager("Keda.Scaler.DurableTask.AzureStorage.Resources.Validation", typeof(SR).Assembly);
}
