// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Security;

internal static class StreamWriterExtensions
{
    public static void WritePrivateKey(this StreamWriter writer, RSA key)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(key);

        writer.WriteLine("-----BEGIN RSA PRIVATE KEY-----");
        writer.WriteLine(Convert.ToBase64String(key.ExportRSAPrivateKey(), Base64FormattingOptions.InsertLineBreaks));
        writer.WriteLine("-----END RSA PRIVATE KEY-----");
    }

    public static void WriteBase64Cert(this StreamWriter writer, X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(certificate);

        writer.WriteLine("-----BEGIN CERTIFICATE-----");
        writer.WriteLine(Convert.ToBase64String(certificate.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks));
        writer.WriteLine("-----END CERTIFICATE-----");
    }
}
