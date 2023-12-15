// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Globalization;
using System.Resources;

namespace Keda.Scaler.DurableTask.AzureStorage;

internal static partial class SR
{
    public static string AmbiguousConnectionOptionFormat => ValidationResourceManager.GetString(nameof(AmbiguousConnectionOptionFormat), CultureInfo.CurrentUICulture)!;

    public static string AmbiguousIdentityCredentialMessage => ValidationResourceManager.GetString(nameof(AmbiguousIdentityCredentialMessage), CultureInfo.CurrentUICulture)!;

    public static string BlankConnectionVarFormat => ValidationResourceManager.GetString(nameof(BlankConnectionVarFormat), CultureInfo.CurrentUICulture)!;

    public static string FileNotFoundFormat => ValidationResourceManager.GetString(nameof(FileNotFoundFormat), CultureInfo.CurrentUICulture)!;

    public static string InvalidConnectionStringOptionFormat => ValidationResourceManager.GetString(nameof(InvalidConnectionStringOptionFormat), CultureInfo.CurrentUICulture)!;

    public static string InvalidPropertyTypeFormat => ValidationResourceManager.GetString(nameof(InvalidPropertyTypeFormat), CultureInfo.CurrentUICulture)!;

    public static string MissingCertificateMessage => ValidationResourceManager.GetString(nameof(MissingCertificateMessage), CultureInfo.CurrentUICulture)!;

    public static string MissingIdentityCredentialOptionFormat => ValidationResourceManager.GetString(nameof(MissingIdentityCredentialOptionFormat), CultureInfo.CurrentUICulture)!;

    public static string OptionalBlankValueFormat => ValidationResourceManager.GetString(nameof(OptionalBlankValueFormat), CultureInfo.CurrentUICulture)!;

    public static string PrivateCloudOnlyFieldFormat => ValidationResourceManager.GetString(nameof(PrivateCloudOnlyFieldFormat), CultureInfo.CurrentUICulture)!;

    public static string PrivateCloudRequiredFieldFormat => ValidationResourceManager.GetString(nameof(PrivateCloudRequiredFieldFormat), CultureInfo.CurrentUICulture)!;

    public static string UnknownValueFormat => ValidationResourceManager.GetString(nameof(UnknownValueFormat), CultureInfo.CurrentUICulture)!;

    private static readonly ResourceManager ValidationResourceManager = new("Keda.Scaler.DurableTask.AzureStorage.Resources.Validation", typeof(SR).Assembly);
}
