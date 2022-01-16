// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using k8s;

namespace Keda.Scaler.DurableTask.AzureStorage.Kubernetes;

#nullable disable

internal sealed class V1ScaleTargetRef : IKubernetesObject, IValidate
{
    public string ApiVersion { get; set; }

    public string Kind { get; set; }

    public string Name { get; set; }

    public void Validate()
    {
        if (Name is null)
            throw new ArgumentNullException(nameof(Name));

        if (ApiVersion is not null)
        {
            string[] sections = ApiVersion.Split(new char[] { '/' }, StringSplitOptions.None);
            if (sections.Length != 2)
                throw new ArgumentException(SR.Format(SR.InvalidApiVersionFormat, ApiVersion), nameof(ApiVersion));
        }
    }
}

#nullable enable
