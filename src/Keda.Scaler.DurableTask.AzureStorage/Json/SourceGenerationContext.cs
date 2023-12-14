// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using Keda.Scaler.DurableTask.AzureStorage.TaskHub;

namespace Keda.Scaler.DurableTask.AzureStorage.Json;

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    ReadCommentHandling = JsonCommentHandling.Skip)]
[JsonSerializable(typeof(AzureStorageTaskHubInfo))]
internal partial class SourceGenerationContext : JsonSerializerContext
{ }
