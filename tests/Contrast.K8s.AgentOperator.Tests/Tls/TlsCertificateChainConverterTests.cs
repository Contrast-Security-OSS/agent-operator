using AutoFixture;
using CertificateManager;
using Contrast.K8s.AgentOperator.Core.Tls;
using Contrast.K8s.AgentOperator.Options;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Tls
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

        private TlsCertificateChain FakeCertificates()
        {
            var generator = new TlsCertificateChainGenerator(new CreateCertificates(new CertificateUtility()));
            return generator.CreateTlsCertificateChain(AutoFixture.Create<TlsCertificateOptions>());
        }
    }
}
