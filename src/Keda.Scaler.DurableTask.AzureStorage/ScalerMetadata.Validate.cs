// Copyright © William Sugarman.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage;

[OptionsValidator]
internal partial class ValidateScalerMetadata : IValidateOptions<ScalerMetadata>
{ }
