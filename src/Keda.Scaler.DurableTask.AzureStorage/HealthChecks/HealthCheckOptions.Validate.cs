// Copyright © William Sugarman.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.HealthChecks;

[OptionsValidator]
internal partial class ValidateHealthCheckOptions : IValidateOptions<HealthCheckOptions>
{ }
