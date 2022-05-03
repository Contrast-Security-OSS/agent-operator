using AutoFixture;
using CertificateManager;
using Contrast.K8s.AgentOperator.Core.Tls;
using Contrast.K8s.AgentOperator.Options;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Core.Tls
{
    public class TlsCertificateChainConverterTests
    {
        private static readonly Fixture AutoFixture = new();

        [Fact]
        public void Round_trip()
        {
            var converter = new TlsCertificateChainConverter();
            using var chainFake = FakeCertificates();

            // Act
            using var result = converter.Import(converter.Export(chainFake));

            // Assert
            using (new AssertionScope())
            {
                result.CaCertificate.SubjectName.Name.Should().Be(chainFake.CaCertificate.SubjectName.Name);
                result.ServerCertificate.HasPrivateKey.Should().BeTrue();
            }
        }

        [Fact]
        public void When_exported_ca_pem_should_not_have_a_bom()
        {
            var converter = new TlsCertificateChainConverter();
            using var chainFake = FakeCertificates();

            // Act
            var result = converter.Export(chainFake);

            // Assert
            using (new AssertionScope())
            {
                result.CaPublicPem[..3].Should().NotBeEquivalentTo(new byte[] { 0xEF, 0xBB, 0xBF });
            }
        }

        [Fact]
        public void Certificates_should_be_exportable_after_being_imported()
        {
            var converter = new TlsCertificateChainConverter();
            using var chainFake = FakeCertificates();
            using var importedFake = converter.Import(converter.Export(chainFake));

            // Act
            var result = converter.Export(importedFake);

            // Assert
            using (new AssertionScope())
            {
                result.CaCertificatePfx.Should().NotBeNullOrEmpty();
                result.ServerCertificatePfx.Should().NotBeNullOrEmpty();
                result.CaPublicPem.Should().NotBeNullOrEmpty();
            }
        }

        private TlsCertificateChain FakeCertificates()
        {
            var generator = new TlsCertificateChainGenerator(new CreateCertificates(new CertificateUtility()), AutoFixture.Create<TlsCertificateOptions>());
            return generator.CreateTlsCertificateChain();
        }
    }
}
