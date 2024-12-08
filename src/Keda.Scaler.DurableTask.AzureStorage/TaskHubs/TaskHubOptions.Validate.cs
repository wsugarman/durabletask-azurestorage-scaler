// Copyright © William Sugarman.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.TaskHubs;

[OptionsValidator]
internal partial class ValidateTaskHubOptions : IValidateOptions<TaskHubOptions>
{ }
