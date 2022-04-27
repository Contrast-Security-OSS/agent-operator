using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace Contrast.K8s.AgentOperator.Core.Tls
{
    public interface ITlsCertificateChainConverter
    {
        TlsCertificateChainExport Export(TlsCertificateChain chain);
        TlsCertificateChain Import(TlsCertificateChainExport export);
    }

    public class TlsCertificateChainConverter : ITlsCertificateChainConverter
    {
        public TlsCertificateChainExport Export(TlsCertificateChain chain)
        {
            var caCertificatePem = chain.CaCertificate.Export(X509ContentType.Pkcs12);

            var caPublic = DotNetUtilities.FromX509Certificate(chain.CaCertificate);
            var caPublicPem = CreatePem(caPublic);

            var serverCertificatePem = chain.ServerCertificate.Export(X509ContentType.Pkcs12);

            return new TlsCertificateChainExport(caCertificatePem, caPublicPem, serverCertificatePem);
        }

        public TlsCertificateChain Import(TlsCertificateChainExport export)
        {
            var (caCertificatePem, _, serverCertificatePem) = export;

            var caCertificate = new X509Certificate2(caCertificatePem, (string?)null, X509KeyStorageFlags.Exportable);
            var serverCertificate = new X509Certificate2(serverCertificatePem, (string?)null, X509KeyStorageFlags.Exportable);

            return new TlsCertificateChain(caCertificate, serverCertificate);
        }

        private static byte[] CreatePem(object o)
        {
            using var memory = new MemoryStream();
            using var writer = new StreamWriter(memory, Encoding.UTF8);
            new PemWriter(writer).WriteObject(o);
            writer.Flush();

            return memory.ToArray();
        }
    }

    public record TlsCertificateChainExport(byte[] CaCertificatePfx, byte[] CaPublicPem, byte[] ServerCertificatePfx);
}
