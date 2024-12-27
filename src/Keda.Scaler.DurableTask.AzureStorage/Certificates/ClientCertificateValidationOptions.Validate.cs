// Copyright © William Sugarman.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.Certificates;

[OptionsValidator]
internal partial class ValidateClientCertificateValidationOptions : IValidateOptions<ClientCertificateValidationOptions>
{
    public static ValidateClientCertificateValidationOptions Instance { get; } = new();
}
