// Copyright © William Sugarman.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.Security;

[OptionsValidator]
internal partial class ValidateTlsServerOptions : IValidateOptions<TlsServerOptions>
{ }
