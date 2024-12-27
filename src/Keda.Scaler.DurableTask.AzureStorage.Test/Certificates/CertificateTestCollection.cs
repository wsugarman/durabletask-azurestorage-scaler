// Copyright © William Sugarman.
// Licensed under the MIT License.

using Xunit;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Certificates;

[CollectionDefinition(nameof(CertificateTestCollection), DisableParallelization = true)]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Suffix used for test collection.")]
public class CertificateTestCollection
{ }
