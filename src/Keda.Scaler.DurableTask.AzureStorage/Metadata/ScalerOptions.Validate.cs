// Copyright © William Sugarman.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.Metadata;

[OptionsValidator]
internal partial class ValidateTaskHubScalerOptions : IValidateOptions<ScalerOptions>
{ }
