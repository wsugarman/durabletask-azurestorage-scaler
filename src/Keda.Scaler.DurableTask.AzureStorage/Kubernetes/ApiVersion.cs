// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;

namespace Keda.Scaler.DurableTask.AzureStorage.Kubernetes;

internal static class ApiVersion
{
    public static (string Group, string Version) Split(string apiVersion)
    {
        if (apiVersion is null)
            throw new ArgumentNullException(nameof(apiVersion));

        string[] sections = apiVersion.Split(new char[] { '/' }, StringSplitOptions.None);
        if (sections.Length != 2)
            throw new ArgumentException(SR.Format(SR.InvalidApiVersionFormat, apiVersion), nameof(apiVersion));

        return (sections[0], sections[1]);
    }
}
