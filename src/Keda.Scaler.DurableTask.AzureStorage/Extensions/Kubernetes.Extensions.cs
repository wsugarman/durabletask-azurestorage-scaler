// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using Keda.Scaler.DurableTask.AzureStorage.Kubernetes;
using Microsoft.Rest;

namespace Keda.Scaler.DurableTask.AzureStorage.Extensions;

internal static class KubernetesExtensions
{
    private const string KedaApiGroup = "keda.sh";
    private const string KedaScaledObjectVersion = "v1alpha1";
    private const string KedaScaledObjectKind = "scaledobject";
    private const string KedaScaledObjectKindExact = KedaScaledObjectKind + "." + KedaApiGroup;
    private const string KedaScaledObjectPlural = "scaledobjects";

    private static readonly JsonSerializerOptions JsonSerializerOptions = CreateJsonSerializerOptions();

    public static async ValueTask<V1Scale> ReadNamespacedCustomObjectScaleAsync(
        this IKubernetes kubernetes,
        string name,
        string namespaceParameter,
        string group,
        string version,
        string kind,
        CancellationToken cancellationToken = default)
    {
        if (kubernetes is null)
            throw new ArgumentNullException(nameof(kubernetes));

        if (name is null)
            throw new ArgumentNullException(nameof(name));

        if (namespaceParameter is null)
            throw new ArgumentNullException(nameof(namespaceParameter));

        if (group is null)
            throw new ArgumentNullException(nameof(group));

        if (version is null)
            throw new ArgumentNullException(nameof(version));

        if (kind is null)
            throw new ArgumentNullException(nameof(kind));

        // Note: We're assuming the plural simply has an 's' at the end, which may be incorrect for custom resources
        object obj = await kubernetes.GetNamespacedCustomObjectScaleAsync(
            group,
            version,
            namespaceParameter,
            kind + 's',
            name,
            cancellationToken).ConfigureAwait(false);

        V1Scale? scale = obj is JsonElement element && element.ValueKind == JsonValueKind.Object
            ? element.Deserialize<V1Scale>(JsonSerializerOptions)
            : null;

        if (scale is null)
            throw new SerializationException(SR.Format(SR.JsonParseFormat, nameof(V1ScaledObject)));

        return scale.Validate(
            kind + "." + group,
            name,
            namespaceParameter,
            s =>
            {
#pragma warning disable CA2208 // This is by convention in Kubernetes.Client
                if (s.Status is null)
                    throw new ArgumentNullException(nameof(V1Scale.Status));
#pragma warning restore CA2208
            });
    }

    public static async ValueTask<V1ScaledObject> ReadNamespacedScaledObjectAsync(
        this IKubernetes kubernetes,
        string name,
        string namespaceParameter,
        CancellationToken cancellationToken = default)
    {
        if (kubernetes is null)
            throw new ArgumentNullException(nameof(kubernetes));

        if (name is null)
            throw new ArgumentNullException(nameof(name));

        if (namespaceParameter is null)
            throw new ArgumentNullException(nameof(namespaceParameter));

        object obj = await kubernetes.GetNamespacedCustomObjectAsync(
            KedaApiGroup,
            KedaScaledObjectVersion,
            namespaceParameter,
            KedaScaledObjectPlural,
            name,
            cancellationToken).ConfigureAwait(false);

        V1ScaledObject? scaledObject = obj is JsonElement element && element.ValueKind == JsonValueKind.Object
            ? element.Deserialize<V1ScaledObject>(JsonSerializerOptions)
            : null;

        if (scaledObject is null)
            throw new SerializationException(SR.Format(SR.JsonParseFormat, nameof(V1ScaledObject)));

        return scaledObject.Validate(KedaScaledObjectKindExact, name, namespaceParameter);
    }

    private static T Validate<T>(this T obj, string kind, string name, string namespaceParameter, Action<T>? supplemental = null)
        where T : IValidate
    {
        try
        {
            obj.Validate();
            supplemental?.Invoke(obj);
            return obj;
        }
        catch (Exception ex)
        {
            throw new ArgumentException(
                SR.Format(
                    SR.InvalidK8sResourceFormat,
                    kind,
                    name,
                    namespaceParameter,
                    ex.Message));
        }
    }

    private static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        // TODO: Add any Kubernetes-specific converters as necessary
        options.Converters.Add(new JsonStringEnumConverter());

        return options;
    }
}
