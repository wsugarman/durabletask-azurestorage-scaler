// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using Keda.Scaler.DurableTask.AzureStorage.Security;
using Xunit.Abstractions;

namespace Keda.Scaler.DurableTask.AzureStorage.Test.Security;

public class TlsCertificateTest : FileSystemTest
{
    private const string CaCertName = "ca.crt";
    private const string ServerCertName = "server.pem";
    private const string ServerKeyName = "server.key";

    private readonly CertificateFile _caCertificate;
    private readonly CertificateFile _serverCertificate;

    public string CaCertPath { get; }

    public string ServerCertPath { get; }

    public string ServerKeyPath { get; }

    internal CertificateFileMonitor ClientCa { get; }

    internal CertificateFileMonitor Server { get; }

    public TlsCertificateTest(ITestOutputHelper outputHelper)
        : base(outputHelper)
    {
        CaCertPath = Path.Combine(RootFolder, CaCertName);
        ServerCertPath = Path.Combine(RootFolder, ServerCertName);
        ServerKeyPath = Path.Combine(RootFolder, ServerKeyName);

        using RSA caCertKey = RSA.Create();
        using RSA serverKey = RSA.Create();
        using X509Certificate2 caCertificate = caCertKey.CreateSelfSignedCertificate();
        using X509Certificate2 serverCertificate = serverKey.CreateCertificate(caCertificate, nameof(TlsConfigureTest));

        File.WriteAllText(CaCertPath, caCertificate.ExportCertificatePem());
        File.WriteAllText(ServerCertPath, serverCertificate.ExportCertificatePem());
        File.WriteAllText(ServerKeyPath, serverKey.ExportRSAPrivateKeyPem());

        _caCertificate = new CertificateFile(CaCertPath);
        _serverCertificate = CertificateFile.CreateFromPemFile(ServerCertPath, ServerKeyPath);

        ClientCa = _caCertificate.Monitor(Logger);
        Server = _serverCertificate.Monitor(Logger);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ClientCa.Dispose();
            Server.Dispose();

            _caCertificate.Dispose();
            _serverCertificate.Dispose();
        }

        base.Dispose(disposing);
    }
}
