// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

            return new TlsCertificateChainExport(caCertificatePem, caPublicPem, serverCertificatePem, chain.SanDnsNamesHash, chain.Version);
        }

        public TlsCertificateChain Import(TlsCertificateChainExport export)
        {
            var caCertificate = new X509Certificate2(export.CaCertificatePfx, (string?)null, X509KeyStorageFlags.Exportable);
            var serverCertificate = new X509Certificate2(export.ServerCertificatePfx, (string?)null, X509KeyStorageFlags.Exportable);

            return new TlsCertificateChain(caCertificate, serverCertificate, export.SansHash, export.Version);
        }

        private static byte[] CreatePem(object o)
        {
            using var memory = new MemoryStream();
            using var writer = new StreamWriter(memory, Encoding.ASCII);
            new PemWriter(writer).WriteObject(o);
            writer.Flush();

            return memory.ToArray();
        }
    }

    public record TlsCertificateChainExport(byte[] CaCertificatePfx, byte[] CaPublicPem, byte[] ServerCertificatePfx, byte[] SansHash, byte[] Version);
}
