// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CertificateManager;
using CertificateManager.Models;
using Contrast.K8s.AgentOperator.Options;

namespace Contrast.K8s.AgentOperator.Core.Tls
{
    public interface ITlsCertificateChainGenerator
    {
        TlsCertificateChain CreateTlsCertificateChain();
    }

    public class TlsCertificateChainGenerator : ITlsCertificateChainGenerator
    {
        public static readonly byte[] GenerationVersion = { 3 };

        private readonly CreateCertificates _createCertificates;
        private readonly TlsCertificateOptions _options;

        public TlsCertificateChainGenerator(CreateCertificates createCertificates, TlsCertificateOptions options)
        {
            _createCertificates = createCertificates;
            _options = options;
        }

        public TlsCertificateChain CreateTlsCertificateChain()
        {
            var ca = CreateRootCa(_options);
            var serverCertificate = CreateServerCertificate(ca);

            return new TlsCertificateChain(ca, serverCertificate, GenerateSansHash(_options.SanDnsNames), GenerationVersion);
        }

        private X509Certificate2 CreateRootCa(TlsCertificateOptions options)
        {
            var distinguishedName = new DistinguishedName
            {
                CommonName = $"{options.NamePrefix}-ca"
            };
            var validityPeriod = new ValidityPeriod
            {
                ValidFrom = DateTimeOffset.Now.AddDays(-1),
                ValidTo = DateTimeOffset.Now + options.ExpiresAfter
            };

            var enhancedKeyUsages = new OidCollection
            {
                OidLookup.ClientAuthentication,
                OidLookup.ServerAuthentication
            };

            var basicConstraints = new BasicConstraints
            {
                CertificateAuthority = true,
                HasPathLengthConstraint = true,
                PathLengthConstraint = 3,
                Critical = true
            };

            var subjectAlternativeName = new SubjectAlternativeName
            {
                DnsName = new List<string>
                {
                    distinguishedName.CommonName
                }
            };

            var rootCert = _createCertificates.NewRsaSelfSignedCertificate(
                distinguishedName,
                basicConstraints,
                validityPeriod,
                subjectAlternativeName,
                enhancedKeyUsages,
                X509KeyUsageFlags.KeyCertSign,
                CreateRsaConfiguration());

            return rootCert;
        }

        private X509Certificate2 CreateServerCertificate(X509Certificate2 signingCertificate)
        {
            var distinguishedName = new DistinguishedName
            {
                CommonName = $"{_options.NamePrefix}-sever"
            };

            var validityPeriod = new ValidityPeriod
            {
                ValidFrom = DateTimeOffset.Now.AddDays(-1),
                ValidTo = DateTimeOffset.Now + _options.ExpiresAfter
            };

            var enhancedKeyUsages = new OidCollection
            {
                OidLookup.ClientAuthentication,
                OidLookup.ServerAuthentication
            };

            var basicConstraints = new BasicConstraints
            {
                CertificateAuthority = true,
                HasPathLengthConstraint = true,
                PathLengthConstraint = 2,
                Critical = true
            };

            var subjectAlternativeName = new SubjectAlternativeName
            {
                DnsName = _options.SanDnsNames.ToList()
            };

            var intermediateCert = _createCertificates.NewRsaChainedCertificate(
                distinguishedName,
                basicConstraints,
                validityPeriod,
                subjectAlternativeName,
                signingCertificate,
                enhancedKeyUsages,
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                CreateRsaConfiguration());

            return intermediateCert;
        }

        private static RsaConfiguration CreateRsaConfiguration() => new()
        {
            RSASignaturePadding = RSASignaturePadding.Pkcs1,
            HashAlgorithmName = HashAlgorithmName.SHA256,
            KeySize = 2048
        };

        private static byte[] GenerateSansHash(IEnumerable<string> sans)
        {
            // This string needs to be stable.
            var sansStr = string.Join(";", sans.DistinctBy(x => x, StringComparer.Ordinal).OrderBy(x => x));
            return Sha256(sansStr);
        }

        private static byte[] Sha256(string text)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
            return bytes;
        }
    }
}
