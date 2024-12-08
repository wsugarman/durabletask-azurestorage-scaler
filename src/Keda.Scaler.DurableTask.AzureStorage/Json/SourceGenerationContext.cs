// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using Keda.Scaler.DurableTask.AzureStorage.TaskHubs;

namespace Keda.Scaler.DurableTask.AzureStorage.Json;

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Default,
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    ReadCommentHandling = JsonCommentHandling.Skip)]
[JsonSerializable(typeof(BlobPartitionManager.AzureStorageTaskHubInfo))]
internal partial class SourceGenerationContext : JsonSerializerContext
{ }
